using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KdxDesigner.Models;
using KdxDesigner.Services.Access;
using KdxDesigner.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KdxDesigner.ViewModels
{
    public partial class ProcessFlowViewModel : ObservableObject
    {
        private readonly IAccessRepository _repository;
        private ProcessFlowNode? _draggedNode;
        private Point _dragOffset;
        private ProcessFlowNode? _connectionStartNode;
        private bool _isCreatingConnection;
        private bool _isSelecting;
        private Point _selectionStartPoint;
        private List<ProcessFlowNode> _selectedNodes = new();
        private Dictionary<ProcessFlowNode, Point> _dragOffsets = new();
        
        [ObservableProperty] private ObservableCollection<ProcessFlowNode> _nodes = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _connections = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowNode> _allNodes = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _allConnections = new();
        [ObservableProperty] private ProcessFlowNode? _selectedNode;
        [ObservableProperty] private ProcessFlowConnection? _selectedConnection;
        [ObservableProperty] private int _cycleId;
        
        // 選択されたノードのプロパティ
        [ObservableProperty] private string _selectedNodeDetailName = "";
        [ObservableProperty] private int? _selectedNodeOperationId;
        [ObservableProperty] private string _selectedNodeStartSensor = "";
        [ObservableProperty] private string _selectedNodeFinishSensor = "";
        [ObservableProperty] private int? _selectedNodeCategoryId;
        [ObservableProperty] private int? _selectedNodeBlockNumber;
        [ObservableProperty] private string _selectedNodeSkipMode = "";
        [ObservableProperty] private int? _selectedNodeSortNumber;
        [ObservableProperty] private string _selectedNodeComment = "";
        [ObservableProperty] private string _selectedNodeILStart = "";
        [ObservableProperty] private int? _selectedNodeId;
        [ObservableProperty] private string _selectedNodeDisplayName = "";
        [ObservableProperty] private bool _isNodeSelected = false;
        
        [ObservableProperty] private Point _mousePosition;
        [ObservableProperty] private bool _isConnecting;
        [ObservableProperty] private Point _connectionStartPoint;
        [ObservableProperty] private bool _isFiltered = false;
        [ObservableProperty] private ProcessFlowNode? _filterNode;
        [ObservableProperty] private double _canvasWidth = 2000;
        [ObservableProperty] private double _canvasHeight = 2000;
        [ObservableProperty] private bool _isRectangleSelecting = false;
        [ObservableProperty] private Rect _selectionRectangle = new Rect(-1, -1, 0, 0);
        [ObservableProperty] private ObservableCollection<ProcessDetailCategory> _categories = new();
        [ObservableProperty] private ObservableCollection<CompositeProcessGroup> _compositeGroups = new();
        [ObservableProperty] private ObservableCollection<Operation> _operations = new();
        [ObservableProperty] private ObservableCollection<Operation> _filteredOperations = new();
        
        private List<ProcessDetailConnection> _dbConnections = new();
        private List<ProcessDetailFinish> _dbFinishes = new();
        private Dictionary<int, Point> _originalPositions = new();
        
        public ProcessFlowViewModel(IAccessRepository repository)
        {
            _repository = repository;
            
            // 初期値を設定
            IsNodeSelected = false;
            SelectedNodeDisplayName = "";
        }
        
        private ProcessFlowNode CreateNode(ProcessDetail detail, Point position, List<ProcessDetailCategory> categoriesList, Dictionary<int, string> processMap)
        {
            var node = new ProcessFlowNode(detail, position);
            
            // カテゴリ名を設定
            if (detail.CategoryId.HasValue)
            {
                var category = categoriesList.FirstOrDefault(c => c.Id == detail.CategoryId.Value);
                node.CategoryName = category?.CategoryName;
            }
            
            // BlockNumberが設定されている場合、対応する工程名を設定
            if (detail.BlockNumber.HasValue && processMap.ContainsKey(detail.BlockNumber.Value))
            {
                node.CompositeProcessName = processMap[detail.BlockNumber.Value];
            }
            
            System.Diagnostics.Debug.WriteLine($"Added node: {node.DisplayName} at position ({position.X}, {position.Y})");
            
            return node;
        }
        
        public void LoadProcessDetails(int cycleId)
        {
            CycleId = cycleId;
            LoadOperations();
            var details = _repository.GetProcessDetails()
                .Where(d => d.CycleId == cycleId)
                .OrderBy(d => d.SortNumber)
                .ToList();
            
            // カテゴリ情報を取得
            var categoriesList = _repository.GetProcessDetailCategories();
            Categories.Clear();
            foreach (var category in categoriesList)
            {
                Categories.Add(category);
            }
            
            // Process情報を取得（複合工程の情報を含む）
            var processes = _repository.GetProcesses()
                .Where(p => p.CycleId == cycleId)
                .ToList();
            
            // すべての工程のIDとProcessNameのマッピングを作成
            var processMap = processes
                .ToDictionary(p => p.Id, p => p.ProcessName ?? $"工程{p.Id}");
            
            // 中間テーブルから接続情報を取得
            _dbConnections = _repository.GetProcessDetailConnections(cycleId);
            _dbFinishes = _repository.GetProcessDetailFinishes(cycleId);
            
            System.Diagnostics.Debug.WriteLine($"Loading {details.Count} ProcessDetails for CycleId: {cycleId}");
            
            AllNodes.Clear();
            AllConnections.Clear();
            Nodes.Clear();
            Connections.Clear();
            CompositeGroups.Clear();
            
            // ノードを作成し、レイアウトを計算
            var nodeDict = new Dictionary<int, ProcessFlowNode>();
            
            // SortNumberで並べ替えて配置
            var sortedDetails = details.OrderBy(d => d.SortNumber).ToList();
            var levels = CalculateNodeLevels(sortedDetails);
            
            // レベルごとのノード数をカウント
            var levelCounts = new Dictionary<int, int>();
            foreach (var detail in sortedDetails)
            {
                var level = levels.ContainsKey(detail.Id) ? levels[detail.Id] : 0;
                if (!levelCounts.ContainsKey(level))
                    levelCounts[level] = 0;
                levelCounts[level]++;
            }
            
            // 各レベルの現在のノード数を追跡
            var currentLevelCounts = new Dictionary<int, int>();
            double maxX = 0, maxY = 0;
            const double gridSize = 40.0;
            const double nodeSpacing = 120.0;   // ノード間の間隔
            
            foreach (var detail in sortedDetails)
            {
                var level = levels.ContainsKey(detail.Id) ? levels[detail.Id] : 0;
                if (!currentLevelCounts.ContainsKey(level))
                    currentLevelCounts[level] = 0;
                
                var x = level * 240 + 40;
                var y = 40 + currentLevelCounts[level] * nodeSpacing;
                
                var position = new Point(
                    Math.Round(x / gridSize) * gridSize,
                    Math.Round(y / gridSize) * gridSize
                );
                currentLevelCounts[level]++;
                
                maxX = Math.Max(maxX, position.X + 140);
                maxY = Math.Max(maxY, position.Y + 60);
                
                var node = CreateNode(detail, position, categoriesList, processMap);
                AllNodes.Add(node);
                Nodes.Add(node);
                nodeDict[detail.Id] = node;
                _originalPositions[detail.Id] = position;
            }
            
            
            // Canvasサイズを設定（余白を追加）
            CanvasWidth = Math.Max(2000, maxX + 100);
            CanvasHeight = Math.Max(2000, maxY + 100);
            
            // コネクションを作成（中間テーブルから）
            foreach (var conn in _dbConnections)
            {
                if (nodeDict.ContainsKey(conn.FromProcessDetailId) && nodeDict.ContainsKey(conn.ToProcessDetailId))
                {
                    var connection = new ProcessFlowConnection(
                        nodeDict[conn.FromProcessDetailId],
                        nodeDict[conn.ToProcessDetailId]
                    );
                    AllConnections.Add(connection);
                    Connections.Add(connection);
                }
            }
            
            // 期間工程の終了IDからのコネクションを作成
            foreach (var finish in _dbFinishes)
            {
                if (nodeDict.ContainsKey(finish.ProcessDetailId) && nodeDict.ContainsKey(finish.FinishProcessDetailId))
                {
                    var connection = new ProcessFlowConnection(
                        nodeDict[finish.FinishProcessDetailId],  // 終了工程から
                        nodeDict[finish.ProcessDetailId]          // 期間工程へ
                    );
                    connection.IsFinishConnection = true; // 終了条件の接続であることを示す
                    AllConnections.Add(connection);
                    Connections.Add(connection);
                }
            }
        }
        
        private Dictionary<int, int> CalculateNodeLevels(List<ProcessDetail> details)
        {
            var levels = new Dictionary<int, int>();
            var processed = new HashSet<int>();
            
            // 前のノードを持たないノード（開始ノード）はレベル0
            var detailIds = details.Select(d => d.Id).ToHashSet();
            var hasIncomingConnection = new HashSet<int>();
            
            // 中間テーブルから接続先を持つノードを特定
            foreach (var conn in _dbConnections)
            {
                if (detailIds.Contains(conn.ToProcessDetailId))
                {
                    hasIncomingConnection.Add(conn.ToProcessDetailId);
                }
            }
            
            // 接続を持たないノードはレベル0
            foreach (var detail in details.Where(d => !hasIncomingConnection.Contains(d.Id)))
            {
                levels[detail.Id] = 0;
                processed.Add(detail.Id);
            }
            
            // 依存関係に基づいてレベルを計算
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var detail in details.Where(d => !processed.Contains(d.Id)))
                {
                    // 中間テーブルから前のノードを取得
                    var fromIds = _dbConnections
                        .Where(c => c.ToProcessDetailId == detail.Id)
                        .Select(c => c.FromProcessDetailId)
                        .ToList();
                    
                    if (fromIds.Any() && fromIds.All(id => levels.ContainsKey(id)))
                    {
                        levels[detail.Id] = fromIds.Max(id => levels[id]) + 1;
                        processed.Add(detail.Id);
                        changed = true;
                    }
                }
            }
            
            return levels;
        }
        
        partial void OnSelectedNodeChanged(ProcessFlowNode? value)
        {
            
            // 以前のノードの選択を解除
            foreach (var node in AllNodes)
            {
                node.IsSelected = false;
            }
            
            if (value != null)
            {
                // 新しいノードを選択
                value.IsSelected = true;
                
                // すべてのプロパティを更新
                SelectedNodeDetailName = value.ProcessDetail.DetailName ?? "";
                SelectedNodeOperationId = value.ProcessDetail.OperationId;
                SelectedNodeStartSensor = value.ProcessDetail.StartSensor ?? "";
                SelectedNodeFinishSensor = value.ProcessDetail.FinishSensor ?? "";
                SelectedNodeCategoryId = value.ProcessDetail.CategoryId;
                SelectedNodeBlockNumber = value.ProcessDetail.BlockNumber;
                SelectedNodeSkipMode = value.ProcessDetail.SkipMode ?? "";
                SelectedNodeSortNumber = value.ProcessDetail.SortNumber;
                SelectedNodeComment = value.ProcessDetail.Comment ?? "";
                SelectedNodeILStart = value.ProcessDetail.ILStart ?? "";
                SelectedNodeId = value.ProcessDetail.Id;
                SelectedNodeDisplayName = value.DisplayName;
                IsNodeSelected = true;
                
                // FilteredOperationsを更新
                UpdateFilteredOperations();
                
                // 接続の選択を解除
                if (SelectedConnection != null)
                {
                    SelectedConnection.IsSelected = false;
                    SelectedConnection = null;
                }
            }
            else
            {
                // ノードが選択解除されたらプロパティをクリア
                SelectedNodeDetailName = "";
                SelectedNodeOperationId = null;
                SelectedNodeStartSensor = "";
                SelectedNodeFinishSensor = "";
                SelectedNodeCategoryId = null;
                SelectedNodeBlockNumber = null;
                SelectedNodeSkipMode = "";
                SelectedNodeSortNumber = null;
                SelectedNodeComment = "";
                SelectedNodeILStart = "";
                SelectedNodeId = null;
                SelectedNodeDisplayName = "";
                IsNodeSelected = false;
            }
        }
        
        partial void OnSelectedConnectionChanged(ProcessFlowConnection? value)
        {
            // 既存の選択を解除
            foreach (var connection in Connections)
            {
                if (connection != value)
                {
                    connection.IsSelected = false;
                }
            }
            
            // 新しい接続を選択
            if (value != null)
            {
                value.IsSelected = true;
                // ノードの選択を解除
                SelectedNode = null;
                
                // 接続情報ウィンドウを表示するイベントを発生させる
                ConnectionSelected?.Invoke(this, EventArgs.Empty);
            }
        }
        
        public event EventHandler? ConnectionSelected;
        
        private void ClearSelection()
        {
            foreach (var node in _selectedNodes)
            {
                node.IsSelected = false;
            }
            _selectedNodes.Clear();
            _dragOffsets.Clear();
        }
        
        private void UpdateConnectionsForMovedNodes()
        {
            // 移動中のノードのリストを作成
            var movedNodes = new HashSet<ProcessFlowNode>();
            if (_selectedNodes.Count > 0)
            {
                foreach (var node in _selectedNodes)
                {
                    movedNodes.Add(node);
                }
            }
            else if (_draggedNode != null)
            {
                movedNodes.Add(_draggedNode);
            }
            
            // 関連するコネクションの変更通知を発行
            // ProcessFlowConnectionクラスのOnNodePropertyChangedメソッドが
            // 自動的にMidPointの変更通知を発行するため、追加の処理は不要
        }
        
        [RelayCommand]
        private void NodeMouseDown(ProcessFlowNode node)
        {
            
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // Ctrlキーが押されている場合は接続モード
                StartConnection(node);
            }
            else
            {
                // 通常のドラッグモード
                _draggedNode = node;
                
                // Ctrl キーが押されている場合は複数選択モード
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    node.IsSelected = !node.IsSelected;
                    if (node.IsSelected && !_selectedNodes.Contains(node))
                    {
                        _selectedNodes.Add(node);
                    }
                    else if (!node.IsSelected && _selectedNodes.Contains(node))
                    {
                        _selectedNodes.Remove(node);
                    }
                }
                else
                {
                    // Ctrl キーが押されていない場合
                    if (!node.IsSelected)
                    {
                        // このノードが選択されていない場合は、他の選択をクリアしてこのノードを選択
                        ClearSelection();
                        node.IsSelected = true;
                        _selectedNodes.Add(node);
                    }
                }
                
                // 複数選択されている場合は、すべての選択されたノードのオフセットを記録
                if (_selectedNodes.Count > 1)
                {
                    _dragOffsets.Clear();
                    foreach (var selectedNode in _selectedNodes)
                    {
                        _dragOffsets[selectedNode] = new Point(
                            MousePosition.X - selectedNode.Position.X,
                            MousePosition.Y - selectedNode.Position.Y
                        );
                        selectedNode.IsDragging = true;
                        selectedNode.Opacity = 0.7;
                    }
                }
                else
                {
                    // 単一選択の場合
                    _dragOffset = new Point(
                        MousePosition.X - node.Position.X,
                        MousePosition.Y - node.Position.Y
                    );
                    node.IsDragging = true;
                    node.Opacity = 0.7;
                }
                
                SelectedNode = node;
            }
        }
        
        [RelayCommand]
        private void NodeMouseUp(ProcessFlowNode node)
        {
            if (_isCreatingConnection && _connectionStartNode != null && _connectionStartNode != node)
            {
                // 接続を完了
                CreateConnection(_connectionStartNode, node);
            }
            
            if (_selectedNodes.Count > 0)
            {
                foreach (var selectedNode in _selectedNodes)
                {
                    selectedNode.IsDragging = false;
                    selectedNode.Opacity = 1.0;
                }
            }
            else if (_draggedNode != null)
            {
                _draggedNode.IsDragging = false;
                _draggedNode.Opacity = 1.0; // 透明度を元に戻す
            }
            _draggedNode = null;
            EndConnection();
        }
        
        [RelayCommand]
        private void CanvasMouseMove(MouseEventArgs e)
        {
            if (e.Source is IInputElement element)
            {
                var position = e.GetPosition(element);
                MousePosition = position;
                
                if (_isSelecting)
                {
                    // 選択矩形を更新
                    var currentPoint = position;
                    var x = Math.Min(_selectionStartPoint.X, currentPoint.X);
                    var y = Math.Min(_selectionStartPoint.Y, currentPoint.Y);
                    var width = Math.Abs(currentPoint.X - _selectionStartPoint.X);
                    var height = Math.Abs(currentPoint.Y - _selectionStartPoint.Y);
                    SelectionRectangle = new Rect(x, y, width, height);
                    
                    // 矩形内のノードを選択
                    foreach (var node in Nodes)
                    {
                        var nodeRect = new Rect(node.Position.X, node.Position.Y, 140, 60);
                        node.IsSelected = SelectionRectangle.IntersectsWith(nodeRect);
                        if (node.IsSelected && !_selectedNodes.Contains(node))
                        {
                            _selectedNodes.Add(node);
                        }
                        else if (!node.IsSelected && _selectedNodes.Contains(node))
                        {
                            _selectedNodes.Remove(node);
                        }
                    }
                }
                else if (_draggedNode != null)
                {
                    // グリッドサイズ（40ピクセル）
                    const double gridSize = 40.0;
                    
                    if (_selectedNodes.Count > 1 && _selectedNodes.Contains(_draggedNode))
                    {
                        // 複数選択されている場合は、すべての選択されたノードを移動
                        foreach (var node in _selectedNodes)
                        {
                            if (_dragOffsets.ContainsKey(node))
                            {
                                var offset = _dragOffsets[node];
                                var newX = position.X - offset.X;
                                var newY = position.Y - offset.Y;
                                
                                // グリッドにスナップ
                                var snappedX = Math.Round(newX / gridSize) * gridSize;
                                var snappedY = Math.Round(newY / gridSize) * gridSize;
                                
                                node.Position = new Point(snappedX, snappedY);
                            }
                        }
                        
                        // 関連するコネクションを更新
                        UpdateConnectionsForMovedNodes();
                    }
                    else
                    {
                        // 単一ノードの移動
                        var newX = position.X - _dragOffset.X;
                        var newY = position.Y - _dragOffset.Y;
                        
                        // グリッドにスナップ
                        var snappedX = Math.Round(newX / gridSize) * gridSize;
                        var snappedY = Math.Round(newY / gridSize) * gridSize;
                        
                        _draggedNode.Position = new Point(snappedX, snappedY);
                    }
                    
                    // 関連するコネクションを更新
                    UpdateConnectionsForMovedNodes();
                }
            }
        }
        
        [RelayCommand]
        private void CanvasMouseDown(MouseButtonEventArgs e)
        {
            // e.OriginalSource が Canvas の場合のみ処理（ノード上ではない）
            if (e.OriginalSource is Canvas canvas)
            {
                // Canvas上で直接クリックされた場合
                var position = e.GetPosition(canvas);
                _isSelecting = true;
                _selectionStartPoint = position;
                // 選択矩形を開始点に初期化（サイズ0で見えないようにする）
                SelectionRectangle = new Rect(position.X, position.Y, 0, 0);
                IsRectangleSelecting = true;
                
                // Ctrl キーが押されていない場合は、既存の選択をクリア
                if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    ClearSelection();
                }
            }
        }
        
        [RelayCommand]
        private void CanvasMouseUp()
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                IsRectangleSelecting = false;
                // 矩形を画面外に移動してから、サイズを0にする
                SelectionRectangle = new Rect(-1, -1, 0, 0);
            }
            
            if (_selectedNodes.Count > 0)
            {
                foreach (var node in _selectedNodes)
                {
                    node.IsDragging = false;
                    node.Opacity = 1.0;
                }
            }
            else if (_draggedNode != null)
            {
                _draggedNode.IsDragging = false;
                _draggedNode.Opacity = 1.0; // 透明度を元に戻す
            }
            _draggedNode = null;
            EndConnection();
        }
        
        private void StartConnection(ProcessFlowNode node)
        {
            _connectionStartNode = node;
            _isCreatingConnection = true;
            IsConnecting = true;
            ConnectionStartPoint = new Point(
                node.Position.X + 70,
                node.Position.Y + 30
            );
        }
        
        private void EndConnection()
        {
            _connectionStartNode = null;
            _isCreatingConnection = false;
            IsConnecting = false;
        }
        
        private void CreateConnection(ProcessFlowNode from, ProcessFlowNode to)
        {
            // 既存の接続を確認
            var existingConnection = Connections.FirstOrDefault(
                c => c.FromNode == from && c.ToNode == to
            );
            
            if (existingConnection == null)
            {
                // 新しい接続を作成（変更フラグをtrueに設定）
                var connection = new ProcessFlowConnection(from, to, true);
                AllConnections.Add(connection);
                if (!IsFiltered || (FilterNode != null && GetRelatedNodes(FilterNode).Contains(from) && GetRelatedNodes(FilterNode).Contains(to)))
                {
                    Connections.Add(connection);
                }
                
                // 中間テーブルに追加
                var dbConnection = new ProcessDetailConnection
                {
                    FromProcessDetailId = from.Id,
                    ToProcessDetailId = to.Id
                };
                _repository.AddProcessDetailConnection(dbConnection);
            }
        }
        
        [RelayCommand]
        private void DeleteConnection(ProcessFlowConnection connection)
        {
            AllConnections.Remove(connection);
            Connections.Remove(connection);
            
            // 中間テーブルから削除
            _repository.DeleteConnectionsByFromAndTo(connection.FromNode.Id, connection.ToNode.Id);
            
            // 削除によってtoNodeに接続されている他の接続も変更済みとしてマーク
            var toNode = connection.ToNode;
            foreach (var conn in AllConnections.Where(c => c.ToNode == toNode))
            {
                conn.IsModified = true;
            }
            
            // 選択をクリア
            if (SelectedConnection == connection)
            {
                SelectedConnection = null;
            }
        }
        
        [RelayCommand]
        private void SelectConnection(ProcessFlowConnection connection)
        {
            SelectedConnection = connection;
        }
        
        [RelayCommand]
        private void DeleteSelectedConnection()
        {
            if (SelectedConnection != null)
            {
                DeleteConnection(SelectedConnection);
            }
        }
        
        [RelayCommand]
        private void UpdateSelectedNodeProperties()
        {
            if (SelectedNode != null)
            {
                // ProcessDetailのプロパティを更新
                SelectedNode.ProcessDetail.DetailName = SelectedNodeDetailName;
                SelectedNode.ProcessDetail.OperationId = SelectedNodeOperationId;
                SelectedNode.ProcessDetail.StartSensor = SelectedNodeStartSensor;
                SelectedNode.ProcessDetail.FinishSensor = SelectedNodeFinishSensor;
                SelectedNode.ProcessDetail.CategoryId = SelectedNodeCategoryId;
                SelectedNode.ProcessDetail.BlockNumber = SelectedNodeBlockNumber;
                SelectedNode.ProcessDetail.SkipMode = SelectedNodeSkipMode;
                SelectedNode.ProcessDetail.SortNumber = SelectedNodeSortNumber;
                SelectedNode.ProcessDetail.Comment = SelectedNodeComment;
                SelectedNode.ProcessDetail.ILStart = SelectedNodeILStart;
                
                // データベースに保存
                _repository.UpdateProcessDetail(SelectedNode.ProcessDetail);
                
                // カテゴリ名を更新
                if (SelectedNode.ProcessDetail.CategoryId.HasValue)
                {
                    var category = Categories.FirstOrDefault(c => c.Id == SelectedNode.ProcessDetail.CategoryId.Value);
                    SelectedNode.CategoryName = category?.CategoryName;
                }
                else
                {
                    SelectedNode.CategoryName = null;
                }
                
                // BlockNumberが変更された場合、対応する工程名を更新
                if (SelectedNode.ProcessDetail.BlockNumber.HasValue)
                {
                    // Process情報を再取得してすべての工程から工程名を更新
                    var processes = _repository.GetProcesses()
                        .Where(p => p.CycleId == CycleId)
                        .ToList();
                    
                    var process = processes.FirstOrDefault(p => p.Id == SelectedNode.ProcessDetail.BlockNumber.Value);
                    SelectedNode.CompositeProcessName = process?.ProcessName ?? null;
                }
                else
                {
                    SelectedNode.CompositeProcessName = null;
                }
                
                // 表示名が変更された場合はUIを更新
                OnPropertyChanged(nameof(SelectedNode));
            }
        }
        
        private void RefreshConnections()
        {
            // 選択されたノードに関連する接続を再構築
            if (SelectedNode == null) return;
            
            // 選択されたノードへの接続を削除
            var toRemove = Connections.Where(c => c.ToNode == SelectedNode).ToList();
            foreach (var connection in toRemove)
            {
                Connections.Remove(connection);
            }
            
            // 中間テーブルから接続を取得
            var fromIds = _dbConnections
                .Where(c => c.ToProcessDetailId == SelectedNode.Id)
                .Select(c => c.FromProcessDetailId)
                .ToList();
            
            // 接続を作成
            foreach (var fromId in fromIds)
            {
                var fromNode = Nodes.FirstOrDefault(n => n.Id == fromId);
                if (fromNode != null)
                {
                    var connection = new ProcessFlowConnection(fromNode, SelectedNode);
                    Connections.Add(connection);
                }
            }
        }
        
        [RelayCommand]
        private void FilterBySelectedNode()
        {
            if (SelectedNode == null) return;
            
            var relatedNodes = GetRelatedNodes(SelectedNode);
            var relatedNodeIds = new HashSet<int>(relatedNodes.Select(n => n.Id));
            
            // 関連ノードを階層的に再配置
            ArrangeRelatedNodes(SelectedNode, relatedNodes);
            
            // ノードをフィルタリング
            Nodes.Clear();
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = 0, maxY = 0;
            foreach (var node in AllNodes.Where(n => relatedNodeIds.Contains(n.Id)))
            {
                Nodes.Add(node);
                minX = Math.Min(minX, node.Position.X);
                minY = Math.Min(minY, node.Position.Y);
                maxX = Math.Max(maxX, node.Position.X + 140);
                maxY = Math.Max(maxY, node.Position.Y + 40);
            }
            
            // 負の座標を避けるため調整
            if (minX < 40 || minY < 40)
            {
                var offsetX = minX < 40 ? 40 - minX : 0;
                var offsetY = minY < 40 ? 40 - minY : 0;
                
                foreach (var node in Nodes)
                {
                    node.Position = new Point(
                        node.Position.X + offsetX,
                        node.Position.Y + offsetY
                    );
                }
                
                maxX += offsetX;
                maxY += offsetY;
            }
            
            // Canvasサイズを再計算
            CanvasWidth = Math.Max(1000, maxX + 100);
            CanvasHeight = Math.Max(1000, maxY + 100);
            
            // コネクションをフィルタリング
            Connections.Clear();
            foreach (var connection in AllConnections)
            {
                if (relatedNodeIds.Contains(connection.FromNode.Id) && 
                    relatedNodeIds.Contains(connection.ToNode.Id))
                {
                    Connections.Add(connection);
                }
            }
            
            IsFiltered = true;
            FilterNode = SelectedNode;
        }
        
        [RelayCommand]
        private void FilterByDirectNeighbors()
        {
            if (SelectedNode == null) return;
            
            var directNeighbors = GetDirectNeighbors(SelectedNode);
            var neighborIds = new HashSet<int>(directNeighbors.Select(n => n.Id));
            
            // ノードを階層的に配置
            ArrangeDirectNeighbors(SelectedNode, directNeighbors);
            
            // ノードをフィルタリング
            Nodes.Clear();
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = 0, maxY = 0;
            foreach (var node in AllNodes.Where(n => neighborIds.Contains(n.Id)))
            {
                Nodes.Add(node);
                minX = Math.Min(minX, node.Position.X);
                minY = Math.Min(minY, node.Position.Y);
                maxX = Math.Max(maxX, node.Position.X + 140);
                maxY = Math.Max(maxY, node.Position.Y + 40);
            }
            
            // 負の座標を避けるため、必要に応じてすべてのノードを移動
            if (minX < 40 || minY < 40)
            {
                var offsetX = minX < 40 ? 40 - minX : 0;
                var offsetY = minY < 40 ? 40 - minY : 0;
                
                foreach (var node in Nodes)
                {
                    node.Position = new Point(
                        node.Position.X + offsetX,
                        node.Position.Y + offsetY
                    );
                }
                
                // 最大値も調整
                maxX += offsetX;
                maxY += offsetY;
            }
            
            // Canvasサイズを再計算
            CanvasWidth = Math.Max(1000, maxX + 100);
            CanvasHeight = Math.Max(1000, maxY + 100);
            
            // コネクションをフィルタリング
            Connections.Clear();
            foreach (var connection in AllConnections)
            {
                if (neighborIds.Contains(connection.FromNode.Id) && 
                    neighborIds.Contains(connection.ToNode.Id))
                {
                    Connections.Add(connection);
                }
            }
            
            IsFiltered = true;
            FilterNode = SelectedNode;
        }
        
        [RelayCommand]
        private void ResetFilter()
        {
            // すべてのノードを元の位置に戻す
            foreach (var node in AllNodes)
            {
                if (_originalPositions.ContainsKey(node.Id))
                {
                    node.Position = _originalPositions[node.Id];
                }
            }
            
            // すべてのノードとコネクションを表示
            Nodes.Clear();
            double maxX = 0, maxY = 0;
            foreach (var node in AllNodes)
            {
                Nodes.Add(node);
                maxX = Math.Max(maxX, node.Position.X + 140);
                maxY = Math.Max(maxY, node.Position.Y + 40);
            }
            
            // Canvasサイズを再計算
            CanvasWidth = Math.Max(2000, maxX + 100);
            CanvasHeight = Math.Max(2000, maxY + 100);
            
            Connections.Clear();
            foreach (var connection in AllConnections)
            {
                Connections.Add(connection);
            }
            
            IsFiltered = false;
            FilterNode = null;
        }
        
        private HashSet<ProcessFlowNode> GetRelatedNodes(ProcessFlowNode centerNode)
        {
            var relatedNodes = new HashSet<ProcessFlowNode> { centerNode };
            
            // 前方向（このノードが依存しているノード）を追跡
            GetPredecessors(centerNode, relatedNodes);
            
            // 後方向（このノードに依存しているノード）を追跡
            GetSuccessors(centerNode, relatedNodes);
            
            return relatedNodes;
        }
        
        private HashSet<ProcessFlowNode> GetDirectNeighbors(ProcessFlowNode centerNode)
        {
            var neighbors = new HashSet<ProcessFlowNode> { centerNode };
            
            // 直接の前ノードを取得
            var fromIds = _dbConnections
                .Where(c => c.ToProcessDetailId == centerNode.Id)
                .Select(c => c.FromProcessDetailId)
                .ToList();
            
            // ProcessDetailFinishから前ノードを取得（期間工程の場合）
            var finishFromIds = _dbFinishes
                .Where(f => f.ProcessDetailId == centerNode.Id)
                .Select(f => f.FinishProcessDetailId)
                .ToList();
            
            fromIds.AddRange(finishFromIds);
            
            foreach (var fromId in fromIds)
            {
                var predecessorNode = AllNodes.FirstOrDefault(n => n.Id == fromId);
                if (predecessorNode != null)
                {
                    neighbors.Add(predecessorNode);
                }
            }
            
            // 直接の後続ノードを取得
            var toIds = _dbConnections
                .Where(c => c.FromProcessDetailId == centerNode.Id)
                .Select(c => c.ToProcessDetailId)
                .ToList();
            
            // ProcessDetailFinishから後続ノードを取得（終了工程の場合）
            var finishToIds = _dbFinishes
                .Where(f => f.FinishProcessDetailId == centerNode.Id)
                .Select(f => f.ProcessDetailId)
                .ToList();
            
            toIds.AddRange(finishToIds);
            
            foreach (var toId in toIds)
            {
                var successorNode = AllNodes.FirstOrDefault(n => n.Id == toId);
                if (successorNode != null)
                {
                    neighbors.Add(successorNode);
                }
            }
            
            return neighbors;
        }
        
        private void ArrangeRelatedNodes(ProcessFlowNode centerNode, HashSet<ProcessFlowNode> relatedNodes)
        {
            const double gridSize = 40.0;
            const double horizontalSpacing = 240.0;
            const double verticalSpacing = 120.0;
            
            // 各ノードのレベル（深さ）を計算
            var levels = CalculateNodeLevelsFromCenter(centerNode, relatedNodes);
            
            // レベルごとのノード数をカウント
            var levelNodes = new Dictionary<int, List<ProcessFlowNode>>();
            foreach (var node in relatedNodes)
            {
                int level = levels.GetValueOrDefault(node.Id, 0);
                if (!levelNodes.ContainsKey(level))
                    levelNodes[level] = new List<ProcessFlowNode>();
                levelNodes[level].Add(node);
            }
            
            // 各レベルのノードを配置
            foreach (var kvp in levelNodes)
            {
                int level = kvp.Key;
                var nodesAtLevel = kvp.Value;
                
                // ソート（SortNumberを基準に）
                nodesAtLevel = nodesAtLevel.OrderBy(n => n.ProcessDetail.SortNumber ?? 0).ToList();
                
                for (int i = 0; i < nodesAtLevel.Count; i++)
                {
                    var node = nodesAtLevel[i];
                    var x = 400 + level * horizontalSpacing;
                    var y = 200 + (i - (nodesAtLevel.Count - 1) / 2.0) * verticalSpacing;
                    
                    node.Position = new Point(
                        Math.Round(x / gridSize) * gridSize,
                        Math.Round(y / gridSize) * gridSize
                    );
                }
            }
        }
        
        private Dictionary<int, int> CalculateNodeLevelsFromCenter(ProcessFlowNode centerNode, HashSet<ProcessFlowNode> nodes)
        {
            var levels = new Dictionary<int, int>();
            var queue = new Queue<(ProcessFlowNode node, int level)>();
            var visited = new HashSet<int>();
            
            // 中心ノードをレベル0として開始
            queue.Enqueue((centerNode, 0));
            visited.Add(centerNode.Id);
            levels[centerNode.Id] = 0;
            
            while (queue.Count > 0)
            {
                var (currentNode, currentLevel) = queue.Dequeue();
                
                // 前のノードを取得（レベル-1）
                var predecessorIds = _dbConnections
                    .Where(c => c.ToProcessDetailId == currentNode.Id)
                    .Select(c => c.FromProcessDetailId)
                    .ToList();
                
                // ProcessDetailFinishからも取得
                var finishPredecessorIds = _dbFinishes
                    .Where(f => f.ProcessDetailId == currentNode.Id)
                    .Select(f => f.FinishProcessDetailId)
                    .ToList();
                predecessorIds.AddRange(finishPredecessorIds);
                
                foreach (var predId in predecessorIds)
                {
                    if (!visited.Contains(predId) && nodes.Any(n => n.Id == predId))
                    {
                        visited.Add(predId);
                        levels[predId] = currentLevel - 1;
                        var predNode = nodes.First(n => n.Id == predId);
                        queue.Enqueue((predNode, currentLevel - 1));
                    }
                }
                
                // 後のノードを取得（レベル+1）
                var successorIds = _dbConnections
                    .Where(c => c.FromProcessDetailId == currentNode.Id)
                    .Select(c => c.ToProcessDetailId)
                    .ToList();
                
                // ProcessDetailFinishからも取得
                var finishSuccessorIds = _dbFinishes
                    .Where(f => f.FinishProcessDetailId == currentNode.Id)
                    .Select(f => f.ProcessDetailId)
                    .ToList();
                successorIds.AddRange(finishSuccessorIds);
                
                foreach (var succId in successorIds)
                {
                    if (!visited.Contains(succId) && nodes.Any(n => n.Id == succId))
                    {
                        visited.Add(succId);
                        levels[succId] = currentLevel + 1;
                        var succNode = nodes.First(n => n.Id == succId);
                        queue.Enqueue((succNode, currentLevel + 1));
                    }
                }
            }
            
            return levels;
        }
        
        private void ArrangeDirectNeighbors(ProcessFlowNode centerNode, HashSet<ProcessFlowNode> neighbors)
        {
            const double gridSize = 40.0;
            const double horizontalSpacing = 280.0; // 水平間隔
            const double verticalSpacing = 120.0;    // 垂直間隔
            
            // 中心ノードが期間工程かどうかを確認（カテゴリID: 15）
            bool isPeriodProcess = centerNode.ProcessDetail.CategoryId == 15;
            
            // 中心ノードの位置を設定
            var centerX = 400.0;  // 中央に配置
            var centerY = 240.0;  // 上下に余裕を持たせる
            centerNode.Position = new Point(
                Math.Round(centerX / gridSize) * gridSize,
                Math.Round(centerY / gridSize) * gridSize
            );
            
            // 前のノード（開始条件）を取得
            var startPredecessors = new List<ProcessFlowNode>();
            var fromIds = _dbConnections
                .Where(c => c.ToProcessDetailId == centerNode.Id)
                .Select(c => c.FromProcessDetailId)
                .ToList();
            
            foreach (var fromId in fromIds)
            {
                var node = neighbors.FirstOrDefault(n => n.Id == fromId);
                if (node != null && node != centerNode)
                {
                    startPredecessors.Add(node);
                }
            }
            
            // 期間工程の場合、終了条件のノードも取得
            var finishPredecessors = new List<ProcessFlowNode>();
            if (isPeriodProcess)
            {
                var finishFromIds = _dbFinishes
                    .Where(f => f.ProcessDetailId == centerNode.Id)
                    .Select(f => f.FinishProcessDetailId)
                    .ToList();
                
                foreach (var finishId in finishFromIds)
                {
                    var node = neighbors.FirstOrDefault(n => n.Id == finishId);
                    if (node != null && node != centerNode)
                    {
                        finishPredecessors.Add(node);
                    }
                }
            }
            
            // 後のノード（右側に配置）
            var successors = new List<ProcessFlowNode>();
            var toIds = _dbConnections
                .Where(c => c.FromProcessDetailId == centerNode.Id)
                .Select(c => c.ToProcessDetailId)
                .ToList();
            
            // 終了工程として機能する場合の後続ノードも追加
            var finishToIds = _dbFinishes
                .Where(f => f.FinishProcessDetailId == centerNode.Id)
                .Select(f => f.ProcessDetailId)
                .ToList();
            toIds.AddRange(finishToIds);
            
            foreach (var toId in toIds)
            {
                var node = neighbors.FirstOrDefault(n => n.Id == toId);
                if (node != null && node != centerNode)
                {
                    successors.Add(node);
                }
            }
            
            // 期間工程の場合の特別な配置
            if (isPeriodProcess)
            {
                // 開始条件ノードを左上に配置
                for (int i = 0; i < startPredecessors.Count; i++)
                {
                    var x = centerX - horizontalSpacing;
                    var y = centerY - verticalSpacing - (i * verticalSpacing);
                    startPredecessors[i].Position = new Point(
                        Math.Round(x / gridSize) * gridSize,
                        Math.Round(y / gridSize) * gridSize
                    );
                }
                
                // 終了条件ノードを左下に配置
                for (int i = 0; i < finishPredecessors.Count; i++)
                {
                    var x = centerX - horizontalSpacing;
                    var y = centerY + verticalSpacing + (i * verticalSpacing);
                    finishPredecessors[i].Position = new Point(
                        Math.Round(x / gridSize) * gridSize,
                        Math.Round(y / gridSize) * gridSize
                    );
                }
            }
            else
            {
                // 通常の工程の場合、前のノードを左側に縦に並べる
                for (int i = 0; i < startPredecessors.Count; i++)
                {
                    var x = centerX - horizontalSpacing;
                    var y = centerY + (i - (startPredecessors.Count - 1) / 2.0) * verticalSpacing;
                    startPredecessors[i].Position = new Point(
                        Math.Round(x / gridSize) * gridSize,
                        Math.Round(y / gridSize) * gridSize
                    );
                }
            }
            
            // 後のノードを右側に縦に並べる
            for (int i = 0; i < successors.Count; i++)
            {
                var x = centerX + horizontalSpacing;
                var y = centerY + (i - (successors.Count - 1) / 2.0) * verticalSpacing;
                successors[i].Position = new Point(
                    Math.Round(x / gridSize) * gridSize,
                    Math.Round(y / gridSize) * gridSize
                );
            }
        }
        
        private void GetPredecessors(ProcessFlowNode node, HashSet<ProcessFlowNode> visited)
        {
            // 中間テーブルから前のノードを取得
            var fromIds = _dbConnections
                .Where(c => c.ToProcessDetailId == node.Id)
                .Select(c => c.FromProcessDetailId)
                .ToList();
            
            // ProcessDetailFinishから前のノードを取得（このノードが期間工程の場合）
            var finishFromIds = _dbFinishes
                .Where(f => f.ProcessDetailId == node.Id)
                .Select(f => f.FinishProcessDetailId)
                .ToList();
            
            fromIds.AddRange(finishFromIds);
            
            // 前のノードを再帰的に追跡
            foreach (var fromId in fromIds)
            {
                var predecessorNode = AllNodes.FirstOrDefault(n => n.Id == fromId);
                if (predecessorNode != null && visited.Add(predecessorNode))
                {
                    GetPredecessors(predecessorNode, visited);
                }
            }
        }
        
        private void GetSuccessors(ProcessFlowNode node, HashSet<ProcessFlowNode> visited)
        {
            // 中間テーブルから後続ノードを取得
            var toIds = _dbConnections
                .Where(c => c.FromProcessDetailId == node.Id)
                .Select(c => c.ToProcessDetailId)
                .ToList();
            
            // ProcessDetailFinishから後続ノードを取得（このノードが終了工程の場合）
            var finishToIds = _dbFinishes
                .Where(f => f.FinishProcessDetailId == node.Id)
                .Select(f => f.ProcessDetailId)
                .ToList();
            
            toIds.AddRange(finishToIds);
            
            // 後続ノードを取得
            var successors = AllNodes.Where(n => toIds.Contains(n.Id));
            
            foreach (var successor in successors)
            {
                if (visited.Add(successor))
                {
                    GetSuccessors(successor, visited);
                }
            }
        }
        
        [RelayCommand]
        private async Task SaveChanges()
        {
            try
            {
                // 変更をデータベースに保存
                foreach (var node in AllNodes)
                {
                    _repository.UpdateProcessDetail(node.ProcessDetail);
                }
                
                // 保存後、すべての接続の変更フラグをクリア
                foreach (var connection in AllConnections)
                {
                    connection.IsModified = false;
                }
                
                System.Diagnostics.Debug.WriteLine($"ProcessDetail の保存が完了しました。保存件数: {AllNodes.Count}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // エラーハンドリング
                System.Diagnostics.Debug.WriteLine($"保存エラー: {ex.Message}");
                throw; // エラーを上位に伝播
            }
        }
        
        [RelayCommand]
        private void AddNewNode()
        {
            // Canvas中央に新規ノードを配置
            var centerX = CanvasWidth / 2;
            var centerY = CanvasHeight / 2;
            
            // グリッドにスナップ
            const double gridSize = 40.0;
            var snappedX = Math.Round(centerX / gridSize) * gridSize;
            var snappedY = Math.Round(centerY / gridSize) * gridSize;
            
            // 有効なProcessIdを取得
            var processes = _repository.GetProcesses()
                .Where(p => p.CycleId == CycleId)
                .ToList();
            
            if (processes.Count == 0)
            {
                // 適切なプロセスが存在しない場合はエラー
                System.Windows.MessageBox.Show("このサイクルに対応する工程が見つかりません。", "エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            
            // 最初の工程をデフォルトとして使用
            var defaultProcessId = processes.First().Id;
            
            // 新しいProcessDetailオブジェクトを作成
            var newProcessDetail = new ProcessDetail
            {
                ProcessId = defaultProcessId,
                DetailName = "新規工程詳細",
                StartSensor = "",
                CategoryId = 1, // デフォルトカテゴリ
                CycleId = CycleId,
                SortNumber = AllNodes.Count > 0 ? AllNodes.Max(n => n.ProcessDetail.SortNumber ?? 0) + 1 : 1
            };
            
            // データベースに追加
            var newId = _repository.AddProcessDetail(newProcessDetail);
            newProcessDetail.Id = newId;
            
            // カテゴリ情報を取得
            var category = Categories.FirstOrDefault(c => c.Id == newProcessDetail.CategoryId);
            
            // 新しいノードを作成
            var newNode = new ProcessFlowNode(newProcessDetail, new Point(snappedX, snappedY))
            {
                CategoryName = category?.CategoryName
            };
            
            // コレクションに追加
            AllNodes.Add(newNode);
            Nodes.Add(newNode);
            
            // 元の位置を記録
            if (!_originalPositions.ContainsKey(newNode.Id))
            {
                _originalPositions[newNode.Id] = new Point(snappedX, snappedY);
            }
            
            // 新しいノードを選択
            SelectedNode = newNode;
            newNode.IsSelected = true;
            _selectedNodes.Clear();
            _selectedNodes.Add(newNode);
        }
        
        [RelayCommand]
        private void DeleteSelectedNode()
        {
            if (SelectedNode == null) return;
            
            var nodeToDelete = SelectedNode;
            var nodeId = nodeToDelete.Id;
            
            // 削除前に選択をクリア
            SelectedNode = null;
            nodeToDelete.IsSelected = false;
            _selectedNodes.Remove(nodeToDelete);
            
            // 関連するコネクションを削除
            var connectionsToDelete = AllConnections
                .Where(c => c.FromNode.Id == nodeId || c.ToNode.Id == nodeId)
                .ToList();
            
            foreach (var connection in connectionsToDelete)
            {
                AllConnections.Remove(connection);
                Connections.Remove(connection);
                
                // データベースから削除
                _repository.DeleteConnectionsByFromAndTo(connection.FromNode.Id, connection.ToNode.Id);
            }
            
            // ノードをコレクションから削除
            AllNodes.Remove(nodeToDelete);
            Nodes.Remove(nodeToDelete);
            
            // 元の位置情報を削除
            if (_originalPositions.ContainsKey(nodeId))
            {
                _originalPositions.Remove(nodeId);
            }
            
            // データベースからProcessDetailを削除
            _repository.DeleteProcessDetail(nodeId);
        }
        
        [RelayCommand]
        private void EditOperation()
        {
            if (SelectedNode == null) return;
            
            // 選択されたノードのOperationIdからOperationを取得
            Operation? operation = null;
            if (SelectedNodeOperationId.HasValue)
            {
                operation = _repository.GetOperations()
                    .FirstOrDefault(o => o.Id == SelectedNodeOperationId.Value);
            }
            
            // Operationが存在しない場合は新規作成
            if (operation == null)
            {
                operation = new Operation
                {
                    Id = 0,
                    CycleId = CycleId
                };
            }
            
            // Operation編集ダイアログを表示
            var viewModel = new OperationViewModel(operation);
            var dialog = new OperationEditorDialog
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };
            
            bool? dialogResult = false;
            viewModel.SetCloseAction((result) =>
            {
                dialogResult = result;
                dialog.DialogResult = result;
            });
            
            if (dialog.ShowDialog() == true)
            {
                var updatedOperation = viewModel.GetOperation();
                
                // Operationを保存
                if (updatedOperation.Id == 0)
                {
                    // 新規作成
                    var newId = _repository.AddOperation(updatedOperation);
                    updatedOperation.Id = newId;
                    
                    // ProcessDetailのOperationIdを更新
                    SelectedNode.ProcessDetail.OperationId = newId;
                    SelectedNodeOperationId = newId;
                    _repository.UpdateProcessDetail(SelectedNode.ProcessDetail);
                }
                else
                {
                    // 既存のOperationを更新
                    _repository.UpdateOperation(updatedOperation);
                }
                
                // Operationsコレクションを更新
                LoadOperations();
            }
        }
        
        private void LoadOperations()
        {
            Operations.Clear();
            var operations = _repository.GetOperations();
            foreach (var operation in operations)
            {
                Operations.Add(operation);
            }
            
            // フィルタされたOperationsを更新
            UpdateFilteredOperations();
        }
        
        private void UpdateFilteredOperations()
        {
            FilteredOperations.Clear();
            var filteredOps = Operations.Where(o => o.CycleId == CycleId).OrderBy(o => o.OperationName);
            foreach (var operation in filteredOps)
            {
                FilteredOperations.Add(operation);
            }
        }
        
    }
}