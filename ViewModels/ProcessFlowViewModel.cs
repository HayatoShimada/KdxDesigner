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
        private bool _isCreatingFinishConnection;
        private bool _isSelecting;
        private Point _selectionStartPoint;
        private bool _hasMouseMoved;
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
        [ObservableProperty] private int? _selectedNodeStartTimerId;
        [ObservableProperty] private string _selectedNodeDisplayName = "";
        [ObservableProperty] private bool _isNodeSelected = false;
        [ObservableProperty] private int? _selectedNodeProcessId;
        [ObservableProperty] private ObservableCollection<Models.Process> _processes = new();
        
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
        [ObservableProperty] private ObservableCollection<Models.Timer> _availableTimers = new();
        [ObservableProperty] private ObservableCollection<Models.Timer> _filteredTimers = new();
        [ObservableProperty] private string _timerFilterText = "";
        [ObservableProperty] private bool _showOnlyOperationTimers = false;
        [ObservableProperty] private bool _highlightStartSensor = false;
        [ObservableProperty] private bool _highlightStartSensorWithoutTimer = false;
        [ObservableProperty] private bool _canChangeConnectionType = false;
        [ObservableProperty] private bool _showAllConnections = false;
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _incomingConnections = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _outgoingConnections = new();
        [ObservableProperty] private bool _hasOtherCycleConnections = false;
        [ObservableProperty] private ObservableCollection<Cycle> _cycles = new();
        [ObservableProperty] private Cycle? _selectedCycle;
        
        private List<ProcessDetailConnection> _dbConnections = new();
        private List<ProcessDetailFinish> _dbFinishes = new();
        private Dictionary<int, Point> _originalPositions = new();
        
        public ProcessFlowViewModel(IAccessRepository repository)
        {
            _repository = repository;
            
            // 初期値を設定
            IsNodeSelected = false;
            SelectedNodeDisplayName = "";
            
            // サイクル一覧を読み込み
            LoadCycles();
            
            // Process一覧を読み込み
            LoadProcesses();
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
            
            // サイクル一覧を再読み込みして選択状態を更新
            if (SelectedCycle == null || SelectedCycle.Id != cycleId)
            {
                LoadCycles();
            }
            
            // Process一覧を再読み込み
            LoadProcesses();
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
            if (ShowAllConnections)
            {
                // 全ての接続を取得（他サイクルへの接続も含む）
                _dbConnections = _repository.GetAllProcessDetailConnections();
                _dbFinishes = _repository.GetAllProcessDetailFinishes();
            }
            else
            {
                // 現在のサイクルの接続のみ取得
                _dbConnections = _repository.GetProcessDetailConnections(cycleId);
                _dbFinishes = _repository.GetProcessDetailFinishes(cycleId);
            }
            
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
                else if (ShowAllConnections)
                {
                    // 他サイクルへの接続の場合、ダミーノードを作成して表示
                    ProcessFlowNode? fromNode = null;
                    ProcessFlowNode? toNode = null;
                    
                    if (nodeDict.ContainsKey(conn.FromProcessDetailId))
                    {
                        fromNode = nodeDict[conn.FromProcessDetailId];
                        // ToNodeが他サイクルのノード
                        var otherDetail = _repository.GetProcessDetails()
                            .FirstOrDefault(d => d.Id == conn.ToProcessDetailId);
                        if (otherDetail != null)
                        {
                            // ダミーノードをfromNodeの右側に配置
                            var dummyPosition = new Point(
                                fromNode.Position.X + 200,
                                fromNode.Position.Y
                            );
                            toNode = new ProcessFlowNode(otherDetail, dummyPosition);
                            toNode.IsOtherCycleNode = true;
                            AllNodes.Add(toNode);  // AllNodesに追加
                            Nodes.Add(toNode);      // Nodesに追加して表示
                            var connection = new ProcessFlowConnection(fromNode, toNode);
                            connection.IsOtherCycleConnection = true;
                            AllConnections.Add(connection);
                            Connections.Add(connection);
                        }
                    }
                    else if (nodeDict.ContainsKey(conn.ToProcessDetailId))
                    {
                        toNode = nodeDict[conn.ToProcessDetailId];
                        // FromNodeが他サイクルのノード
                        var otherDetail = _repository.GetProcessDetails()
                            .FirstOrDefault(d => d.Id == conn.FromProcessDetailId);
                        if (otherDetail != null)
                        {
                            // ダミーノードをtoNodeの左側に配置
                            var dummyPosition = new Point(
                                toNode.Position.X - 200,
                                toNode.Position.Y
                            );
                            fromNode = new ProcessFlowNode(otherDetail, dummyPosition);
                            fromNode.IsOtherCycleNode = true;
                            AllNodes.Add(fromNode);  // AllNodesに追加
                            Nodes.Add(fromNode);      // Nodesに追加して表示
                            var connection = new ProcessFlowConnection(fromNode, toNode);
                            connection.IsOtherCycleConnection = true;
                            AllConnections.Add(connection);
                            Connections.Add(connection);
                        }
                    }
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
                else if (ShowAllConnections)
                {
                    // 他サイクルへの終了条件接続の場合
                    ProcessFlowNode? fromNode = null;
                    ProcessFlowNode? toNode = null;
                    
                    if (nodeDict.ContainsKey(finish.ProcessDetailId))
                    {
                        toNode = nodeDict[finish.ProcessDetailId];
                        // FinishNodeが他サイクルのノード
                        var otherDetail = _repository.GetProcessDetails()
                            .FirstOrDefault(d => d.Id == finish.FinishProcessDetailId);
                        if (otherDetail != null)
                        {
                            // ダミーノードをtoNodeの左下に配置（終了条件用）
                            var dummyPosition = new Point(
                                toNode.Position.X - 150,
                                toNode.Position.Y + 60
                            );
                            fromNode = new ProcessFlowNode(otherDetail, dummyPosition);
                            fromNode.IsOtherCycleNode = true;
                            var connection = new ProcessFlowConnection(fromNode, toNode);
                            connection.IsFinishConnection = true;
                            connection.IsOtherCycleConnection = true;
                            AllConnections.Add(connection);
                            Connections.Add(connection);
                        }
                    }
                    else if (nodeDict.ContainsKey(finish.FinishProcessDetailId))
                    {
                        fromNode = nodeDict[finish.FinishProcessDetailId];
                        // ProcessDetailが他サイクルのノード
                        var otherDetail = _repository.GetProcessDetails()
                            .FirstOrDefault(d => d.Id == finish.ProcessDetailId);
                        if (otherDetail != null)
                        {
                            // ダミーノードをfromNodeの右上に配置（終了条件から）
                            var dummyPosition = new Point(
                                fromNode.Position.X + 150,
                                fromNode.Position.Y - 60
                            );
                            toNode = new ProcessFlowNode(otherDetail, dummyPosition);
                            toNode.IsOtherCycleNode = true;
                            AllNodes.Add(toNode);  // AllNodesに追加
                            Nodes.Add(toNode);      // Nodesに追加して表示
                            var connection = new ProcessFlowConnection(fromNode, toNode);
                            connection.IsFinishConnection = true;
                            connection.IsOtherCycleConnection = true;
                            AllConnections.Add(connection);
                            Connections.Add(connection);
                        }
                    }
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
                SelectedNodeStartTimerId = value.ProcessDetail.StartTimerId;
                SelectedNodeDisplayName = value.DisplayName;
                IsNodeSelected = true;
                SelectedNodeProcessId = value.ProcessDetail.ProcessId;
                
                // FilteredOperationsを更新
                UpdateFilteredOperations();
                
                // 選択されたノードのOperationIdに基づいてタイマーを読み込む
                LoadAvailableTimers();
                
                // 選択されたノードの接続情報を更新
                UpdateNodeConnections(value);
                
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
                SelectedNodeStartTimerId = null;
                SelectedNodeDisplayName = "";
                IsNodeSelected = false;
                SelectedNodeProcessId = null;
                AvailableTimers.Clear();
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
                
                // 接続種別変更が可能かチェック（ToNodeが期間工程または複合工程の場合）
                CanChangeConnectionType = value.ToNode.ProcessDetail.CategoryId == 15 || 
                                        value.ToNode.ProcessDetail.CategoryId == 18;
                
                // 接続情報ウィンドウを表示するイベントを発生させる
                ConnectionSelected?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                CanChangeConnectionType = false;
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
            
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && 
                (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                // Ctrl+Shiftキーが押されている場合は終了条件接続モード
                StartConnection(node, true);
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // Ctrlキーが押されている場合は通常の接続モード
                StartConnection(node, false);
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
                CreateConnection(_connectionStartNode, node, _isCreatingFinishConnection);
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
                    // マウスが移動したことを記録
                    if (!_hasMouseMoved)
                    {
                        _hasMouseMoved = true;
                        IsRectangleSelecting = true;
                    }
                    
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
                _hasMouseMoved = false;
                // 選択矩形を開始点に初期化（サイズ0で見えないようにする）
                SelectionRectangle = new Rect(position.X, position.Y, 0, 0);
                IsRectangleSelecting = false;  // マウスが移動するまで表示しない
                
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
                _hasMouseMoved = false;
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
        
        private void StartConnection(ProcessFlowNode node, bool isFinishConnection = false)
        {
            _connectionStartNode = node;
            _isCreatingConnection = true;
            _isCreatingFinishConnection = isFinishConnection;
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
            _isCreatingFinishConnection = false;
            IsConnecting = false;
        }
        
        private void CreateConnection(ProcessFlowNode from, ProcessFlowNode to, bool isFinishConnection = false)
        {
            if (isFinishConnection)
            {
                // 終了条件接続の場合
                // fromが終了工程、toが期間工程（カテゴリID=15）または複合工程（カテゴリID=18）である必要がある
                if (to.ProcessDetail.CategoryId != 15 && to.ProcessDetail.CategoryId != 18)
                {
                    System.Windows.MessageBox.Show("終了条件は期間工程または複合工程に対してのみ設定できます。", "エラー", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                // 既存の終了条件接続を確認
                var existingFinishConnection = AllConnections.FirstOrDefault(
                    c => c.FromNode == from && c.ToNode == to && c.IsFinishConnection
                );
                
                if (existingFinishConnection == null)
                {
                    // 新しい終了条件接続を作成
                    var connection = new ProcessFlowConnection(from, to, true);
                    connection.IsFinishConnection = true;
                    AllConnections.Add(connection);
                    if (!IsFiltered || (FilterNode != null && GetRelatedNodes(FilterNode).Contains(from) && GetRelatedNodes(FilterNode).Contains(to)))
                    {
                        Connections.Add(connection);
                    }
                    
                    // ProcessDetailFinishテーブルに追加
                    var dbFinish = new ProcessDetailFinish
                    {
                        ProcessDetailId = to.Id,  // 期間工程
                        FinishProcessDetailId = from.Id  // 終了工程
                    };
                    _repository.AddProcessDetailFinish(dbFinish);
                }
            }
            else
            {
                // 通常の接続の場合
                var existingConnection = Connections.FirstOrDefault(
                    c => c.FromNode == from && c.ToNode == to && !c.IsFinishConnection
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
        }
        
        [RelayCommand]
        private void DeleteConnection(ProcessFlowConnection connection)
        {
            AllConnections.Remove(connection);
            Connections.Remove(connection);
            
            if (connection.IsFinishConnection)
            {
                // 終了条件接続の場合、ProcessDetailFinishから削除
                _repository.DeleteFinishesByProcessAndFinish(connection.ToNode.Id, connection.FromNode.Id);
            }
            else
            {
                // 通常の接続の場合、中間テーブルから削除
                _repository.DeleteConnectionsByFromAndTo(connection.FromNode.Id, connection.ToNode.Id);
            }
            
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
        private async Task UpdateSelectedNodeProperties()
        {
            if (SelectedNode != null)
            {
                try
                {
                // OperationIdがnullまたは0の場合は、現在のProcessDetailの値を維持
                var operationId = SelectedNodeOperationId;
                if (operationId == null || operationId == 0)
                {
                    // 現在のProcessDetailの値を使用
                    operationId = SelectedNode.ProcessDetail.OperationId;
                    // ViewModelのプロパティも更新
                    SelectedNodeOperationId = operationId;
                }
                
                // ProcessDetailのプロパティを更新
                SelectedNode.ProcessDetail.DetailName = SelectedNodeDetailName;
                SelectedNode.ProcessDetail.OperationId = operationId;
                SelectedNode.ProcessDetail.StartSensor = SelectedNodeStartSensor;
                SelectedNode.ProcessDetail.FinishSensor = SelectedNodeFinishSensor;
                SelectedNode.ProcessDetail.CategoryId = SelectedNodeCategoryId;
                SelectedNode.ProcessDetail.BlockNumber = SelectedNodeBlockNumber;
                SelectedNode.ProcessDetail.SkipMode = SelectedNodeSkipMode;
                SelectedNode.ProcessDetail.SortNumber = SelectedNodeSortNumber;
                SelectedNode.ProcessDetail.Comment = SelectedNodeComment;
                SelectedNode.ProcessDetail.ILStart = SelectedNodeILStart;
                SelectedNode.ProcessDetail.StartTimerId = SelectedNodeStartTimerId;
                
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
                
                // UIのプロパティを更新（データベースから読み込んだ値でリフレッシュ）
                var allDetails = _repository.GetProcessDetails();
                var updatedDetail = allDetails.FirstOrDefault(d => d.Id == SelectedNode.ProcessDetail.Id);
                if (updatedDetail != null)
                {
                    // ProcessDetailオブジェクトの値を更新
                    SelectedNode.ProcessDetail.DetailName = updatedDetail.DetailName;
                    SelectedNode.ProcessDetail.OperationId = updatedDetail.OperationId;
                    SelectedNode.ProcessDetail.StartSensor = updatedDetail.StartSensor;
                    SelectedNode.ProcessDetail.FinishSensor = updatedDetail.FinishSensor;
                    SelectedNode.ProcessDetail.CategoryId = updatedDetail.CategoryId;
                    SelectedNode.ProcessDetail.BlockNumber = updatedDetail.BlockNumber;
                    SelectedNode.ProcessDetail.SkipMode = updatedDetail.SkipMode;
                    SelectedNode.ProcessDetail.SortNumber = updatedDetail.SortNumber;
                    SelectedNode.ProcessDetail.Comment = updatedDetail.Comment;
                    SelectedNode.ProcessDetail.ILStart = updatedDetail.ILStart;
                    SelectedNode.ProcessDetail.StartTimerId = updatedDetail.StartTimerId;
                    
                    // ViewModelのプロパティも更新
                    SelectedNodeDetailName = updatedDetail.DetailName ?? "";
                    SelectedNodeOperationId = updatedDetail.OperationId;
                    SelectedNodeStartSensor = updatedDetail.StartSensor ?? "";
                    SelectedNodeFinishSensor = updatedDetail.FinishSensor ?? "";
                    SelectedNodeCategoryId = updatedDetail.CategoryId;
                    SelectedNodeBlockNumber = updatedDetail.BlockNumber;
                    SelectedNodeSkipMode = updatedDetail.SkipMode ?? "";
                    SelectedNodeSortNumber = updatedDetail.SortNumber;
                    SelectedNodeComment = updatedDetail.Comment ?? "";
                    SelectedNodeILStart = updatedDetail.ILStart ?? "";
                    SelectedNodeStartTimerId = updatedDetail.StartTimerId;
                    
                    // 表示名を再度更新
                    SelectedNode.UpdateDisplayName();
                    
                    // 開始センサー関連のプロパティ変更を通知
                    SelectedNode.NotifyStartSensorPropertiesChanged();
                }
                
                // 成功メッセージを表示
                await ShowUpdateSuccessMessage();
            }
            catch (Exception ex)
            {
                // エラーメッセージを表示
                await ShowUpdateErrorMessage(ex.Message);
            }
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
            // 現在選択されているOperationIdを保存
            var currentOperationId = SelectedNodeOperationId;
            
            FilteredOperations.Clear();
            var filteredOps = Operations.Where(o => o.CycleId == CycleId).OrderBy(o => o.OperationName);
            foreach (var operation in filteredOps)
            {
                FilteredOperations.Add(operation);
            }
            
            // FilteredOperationsが更新された後、選択を復元
            if (currentOperationId.HasValue && FilteredOperations.Any(o => o.Id == currentOperationId.Value))
            {
                SelectedNodeOperationId = currentOperationId;
            }
        }


        private void LoadAvailableTimers()
        {
            AvailableTimers.Clear();
            
            // すべてのタイマーを取得
            var allTimers = _repository.GetTimersByCycleId(CycleId);
            
            foreach (var timer in allTimers.OrderBy(t => t.TimerName))
            {
                AvailableTimers.Add(timer);
            }
            
            // フィルタリングされたタイマーを初期表示
            ApplyTimerFilter();
        }

        private void ApplyTimerFilter()
        {
            // 選択されているタイマーIDを一時保存
            var previousSelectedId = SelectedNodeStartTimerId;
            
            FilteredTimers.Clear();
            
            var query = AvailableTimers.AsEnumerable();
            
            // テキストフィルタ
            if (!string.IsNullOrWhiteSpace(TimerFilterText))
            {
                var filterText = TimerFilterText.ToLower();
                query = query.Where(t => 
                    (t.TimerName?.ToLower().Contains(filterText) ?? false) ||
                    (t.ID.ToString().Contains(filterText)));
            }
            
            // Operation関連のタイマーのみ表示
            if (ShowOnlyOperationTimers && SelectedNodeOperationId.HasValue)
            {
                // MnemonicIdが3（Operation）で、RecordIdが選択されたOperationIdのタイマーデバイスを取得
                var timerDevices = _repository.GetTimersByRecordId(CycleId, 3, SelectedNodeOperationId.Value);
                var operationTimerIds = timerDevices.Select(td => td.TimerId).ToHashSet();
                query = query.Where(t => operationTimerIds.Contains(t.ID));
            }
            
            foreach (var timer in query)
            {
                FilteredTimers.Add(timer);
            }
            
            // フィルタリング後も選択状態を維持
            // FilteredTimersに含まれている場合は選択を維持、含まれていない場合はnullにする
            if (previousSelectedId.HasValue && FilteredTimers.Any(t => t.ID == previousSelectedId.Value))
            {
                SelectedNodeStartTimerId = previousSelectedId;
            }
            else if (previousSelectedId.HasValue)
            {
                // フィルタリングで選択していたタイマーが除外された場合
                SelectedNodeStartTimerId = null;
            }
            
            // 選択状態の変更を通知
            OnPropertyChanged(nameof(SelectedNodeStartTimerId));
        }
        
        partial void OnTimerFilterTextChanged(string value)
        {
            ApplyTimerFilter();
        }
        
        partial void OnShowOnlyOperationTimersChanged(bool value)
        {
            ApplyTimerFilter();
        }
        
        partial void OnSelectedNodeStartTimerIdChanged(int? value)
        {
            // タイマーが変更されてもOperationIdは維持する
            // 何もしない - OperationIdの変更を防ぐ
        }
        
        partial void OnSelectedNodeProcessIdChanged(int? value)
        {
            if (SelectedNode?.ProcessDetail != null)
            {
                SelectedNode.ProcessDetail.ProcessId = value ?? 0;
                
                // 変更を追跡（必要に応じて保存フラグを立てる）
                // _hasChanges = true;
            }
        }
        
        partial void OnShowAllConnectionsChanged(bool value)
        {
            // 全接続表示の切り替え時に再読み込み
            LoadProcessDetails(CycleId);
        }
        
        partial void OnSelectedCycleChanged(Cycle? value)
        {
            if (value != null)
            {
                // 選択されたサイクルで工程フローを読み込み
                LoadProcessDetails(value.Id);
            }
        }
        
        private void LoadCycles()
        {
            try
            {
                var cycles = _repository.GetCycles();
                Cycles.Clear();
                foreach (var cycle in cycles.OrderBy(c => c.Id))
                {
                    Cycles.Add(cycle);
                }
                
                // 現在のサイクルを選択
                if (CycleId > 0)
                {
                    SelectedCycle = Cycles.FirstOrDefault(c => c.Id == CycleId);
                }
            }
            catch (Exception ex)
            {
                // エラー処理
                System.Diagnostics.Debug.WriteLine($"LoadCycles error: {ex.Message}");
            }
        }
        
        private void LoadProcesses()
        {
            try
            {
                var processes = _repository.GetProcesses();
                Processes.Clear();
                
                // 現在のサイクルのProcessのみをフィルタ
                foreach (var process in processes.Where(p => p.CycleId == CycleId).OrderBy(p => p.SortNumber ?? 0).ThenBy(p => p.Id))
                {
                    Processes.Add(process);
                }
            }
            catch (Exception ex)
            {
                // エラー処理
                System.Diagnostics.Debug.WriteLine($"LoadProcesses error: {ex.Message}");
            }
        }
        
        private async Task ShowUpdateSuccessMessage()
        {
            await Task.Run(() => 
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        "工程詳細の更新が完了しました。",
                        "更新完了",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                });
            });
        }
        
        private async Task ShowUpdateErrorMessage(string errorMessage)
        {
            await Task.Run(() => 
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"工程詳細の更新中にエラーが発生しました。\n\nエラー内容: {errorMessage}",
                        "更新エラー",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                });
            });
        }
        
        private void UpdateNodeConnections(ProcessFlowNode? node)
        {
            IncomingConnections.Clear();
            OutgoingConnections.Clear();
            HasOtherCycleConnections = false;
            
            if (node == null) return;
            
            // 接続元（このノードへの接続）を取得
            var incoming = AllConnections.Where(c => c.ToNode.Id == node.Id).ToList();
            foreach (var conn in incoming.OrderBy(c => c.FromNode.DisplayName))
            {
                IncomingConnections.Add(conn);
                if (conn.IsOtherCycleConnection)
                {
                    HasOtherCycleConnections = true;
                }
            }
            
            // 接続先（このノードからの接続）を取得
            var outgoing = AllConnections.Where(c => c.FromNode.Id == node.Id).ToList();
            foreach (var conn in outgoing.OrderBy(c => c.ToNode.DisplayName))
            {
                OutgoingConnections.Add(conn);
                if (conn.IsOtherCycleConnection)
                {
                    HasOtherCycleConnections = true;
                }
            }
        }
        
        [RelayCommand]
        private void ChangeConnectionType()
        {
            if (SelectedConnection == null || !CanChangeConnectionType) return;
            
            if (SelectedConnection.IsFinishConnection)
            {
                // 終了条件から通常の接続に変更
                // ProcessDetailFinishから削除
                _repository.DeleteFinishesByProcessAndFinish(SelectedConnection.ToNode.Id, SelectedConnection.FromNode.Id);
                
                // ProcessDetailConnectionに追加
                var dbConnection = new ProcessDetailConnection
                {
                    FromProcessDetailId = SelectedConnection.FromNode.Id,
                    ToProcessDetailId = SelectedConnection.ToNode.Id
                };
                _repository.AddProcessDetailConnection(dbConnection);
                
                // フラグを更新
                SelectedConnection.IsFinishConnection = false;
            }
            else
            {
                // 通常の接続から終了条件に変更
                // ProcessDetailConnectionから削除
                _repository.DeleteConnectionsByFromAndTo(SelectedConnection.FromNode.Id, SelectedConnection.ToNode.Id);
                
                // ProcessDetailFinishに追加
                var dbFinish = new ProcessDetailFinish
                {
                    ProcessDetailId = SelectedConnection.ToNode.Id,  // 期間工程
                    FinishProcessDetailId = SelectedConnection.FromNode.Id  // 終了工程
                };
                _repository.AddProcessDetailFinish(dbFinish);
                
                // フラグを更新
                SelectedConnection.IsFinishConnection = true;
            }
            
            // 変更をUIに反映
            OnPropertyChanged(nameof(SelectedConnection));
            // 接続の再選択で変更を反映
            var temp = SelectedConnection;
            SelectedConnection = null;
            SelectedConnection = temp;
        }
        
    }
}