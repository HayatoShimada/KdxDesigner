using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KdxDesigner.Models;
using KdxDesigner.Services.Access;
using KdxDesigner.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KdxDesigner.ViewModels
{
    public partial class ProcessFlowDetailViewModel : ObservableObject
    {
        private readonly IAccessRepository _repository;
        private readonly int _cycleId;
        private ProcessFlowNode? _draggedNode;
        private Point _dragOffset;
        private ProcessFlowNode? _connectionStartNode;
        private bool _isCreatingFinishConnection;
        private bool _isSelecting;
        private Point _selectionStartPoint;
        private bool _hasMouseMoved;
        private List<ProcessFlowNode> _selectedNodes = new();
        private Dictionary<ProcessFlowNode, Point> _dragOffsets = new();
        
        [ObservableProperty] private string _windowTitle;
        [ObservableProperty] private string _cycleName;
        [ObservableProperty] private ObservableCollection<ProcessFlowNode> _nodes = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _connections = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowNode> _allNodes = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _allConnections = new();
        [ObservableProperty] private ProcessFlowNode? _selectedNode;
        [ObservableProperty] private ProcessFlowConnection? _selectedConnection;
        
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
        [ObservableProperty] private double _canvasWidth = 3000;
        [ObservableProperty] private double _canvasHeight = 3000;
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
        private bool _showNodeId = false;
        private bool _showBlockNumber = false;
        
        // ズーム機能用のプロパティ
        [ObservableProperty] private double _zoomScale = 1.0;
        private const double MinZoomScale = 0.1;
        private const double MaxZoomScale = 5.0;
        private const double ZoomStep = 0.1;
        
        public bool ShowNodeId
        {
            get => _showNodeId;
            set
            {
                if (SetProperty(ref _showNodeId, value))
                {
                    UpdateNodeHeights();
                }
            }
        }
        
        public bool ShowBlockNumber
        {
            get => _showBlockNumber;
            set
            {
                if (SetProperty(ref _showBlockNumber, value))
                {
                    UpdateNodeHeights();
                }
            }
        }
        
        private void UpdateNodeHeights()
        {
            double height = 45;
            if (ShowNodeId && ShowBlockNumber)
                height = 60;
            else if (ShowNodeId || ShowBlockNumber)
                height = 55;
            
            foreach (var node in AllNodes)
            {
                node.NodeHeight = height;
            }
            
            // 接続線の位置を更新
            foreach (var connection in AllConnections)
            {
                connection.UpdatePosition();
            }
        }
        
        private List<ProcessDetailConnection> _dbConnections = new();
        private List<ProcessDetailFinish> _dbFinishes = new();
        private Dictionary<int, Point> _originalPositions = new();
        
        public ProcessFlowDetailViewModel(IAccessRepository repository, int cycleId, string cycleName)
        {
            _repository = repository;
            _cycleId = cycleId;
            CycleName = $"{cycleId} - {cycleName}";
            WindowTitle = $"工程フロー詳細 - {CycleName}";
            
            // 初期値を設定
            IsNodeSelected = false;
            SelectedNodeDisplayName = "";
            
            // Process一覧を読み込み
            LoadProcesses();
            LoadOperations();
        }
        
        public async void LoadNodesAsync()
        {
            // データ取得を非同期で行い、UI更新はUIスレッドで実行
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LoadProcessDetails();
            });
        }
        
        private void LoadProcesses()
        {
            var processes = _repository.GetProcesses()
                .Where(p => p.CycleId == _cycleId)
                .OrderBy(p => p.SortNumber)
                .ToList();
            
            Processes.Clear();
            foreach (var process in processes)
            {
                Processes.Add(process);
            }
        }
        
        private void LoadOperations()
        {
            var operations = _repository.GetOperations()
                .OrderBy(o => o.Id)
                .ToList();
            
            Operations.Clear();
            foreach (var operation in operations)
            {
                Operations.Add(operation);
            }
            
            // フィルタされたOperationsを初期化
            FilteredOperations.Clear();
            foreach (var operation in operations)
            {
                FilteredOperations.Add(operation);
            }
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
        
        // 接続選択時のイベント
        public event EventHandler? ConnectionSelected;
        
        public void LoadProcessDetails()
        {
            var details = _repository.GetProcessDetails()
                .Where(d => d.CycleId == _cycleId)
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
                .Where(p => p.CycleId == _cycleId)
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
                _dbConnections = _repository.GetProcessDetailConnections(_cycleId);
                _dbFinishes = _repository.GetProcessDetailFinishes(_cycleId);
            }
            
            System.Diagnostics.Debug.WriteLine($"Loading {details.Count} ProcessDetails for CycleId: {_cycleId}");
            
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
            
            foreach (var detail in sortedDetails)
            {
                var level = levels.ContainsKey(detail.Id) ? levels[detail.Id] : 0;
                if (!currentLevelCounts.ContainsKey(level))
                    currentLevelCounts[level] = 0;
                
                var index = currentLevelCounts[level];
                var totalInLevel = levelCounts[level];
                
                // ノードの位置を計算
                double x = level * 250 + 100; // 水平間隔を250に、左余白を100に
                double y = 150 + (index * 100); // 垂直間隔を100に、上余白を150に
                
                var position = new Point(x, y);
                var node = CreateNode(detail, position, categoriesList, processMap);
                
                nodeDict[detail.Id] = node;
                AllNodes.Add(node);
                Nodes.Add(node);
                
                currentLevelCounts[level]++;
                
                // キャンバスサイズを更新
                maxX = Math.Max(maxX, x + 150);
                maxY = Math.Max(maxY, y + 50);
            }
            
            // キャンバスサイズを設定（余白を追加）
            CanvasWidth = Math.Max(3000, maxX + 400);
            CanvasHeight = Math.Max(3000, maxY + 400);
            
            // 他サイクルのノードも追加（ShowAllConnectionsがtrueの場合）
            if (ShowAllConnections)
            {
                AddOtherCycleNodes(nodeDict, categoriesList, processMap);
            }
            
            // 接続を作成
            CreateConnections(nodeDict);
            
            // 複合工程のグループを作成
            CreateCompositeGroups(nodeDict, processes);
            
            // 元の位置を記録
            _originalPositions.Clear();
            foreach (var node in AllNodes)
            {
                _originalPositions[node.ProcessDetail.Id] = node.Position;
            }
            
            System.Diagnostics.Debug.WriteLine($"Created {AllNodes.Count} nodes and {AllConnections.Count} connections");
        }
        
        private void AddOtherCycleNodes(Dictionary<int, ProcessFlowNode> nodeDict, List<ProcessDetailCategory> categoriesList, Dictionary<int, string> processMap)
        {
            // 他サイクルへの接続を見つける
            var otherCycleDetailIds = new HashSet<int>();
            
            // 接続元と接続先から他サイクルのIDを収集
            foreach (var conn in _dbConnections)
            {
                if (!nodeDict.ContainsKey(conn.FromProcessDetailId))
                    otherCycleDetailIds.Add(conn.FromProcessDetailId);
                if (!nodeDict.ContainsKey(conn.ToProcessDetailId))
                    otherCycleDetailIds.Add(conn.ToProcessDetailId);
            }
            
            foreach (var finish in _dbFinishes)
            {
                if (!nodeDict.ContainsKey(finish.ProcessDetailId))
                    otherCycleDetailIds.Add(finish.ProcessDetailId);
                if (!nodeDict.ContainsKey(finish.FinishProcessDetailId))
                    otherCycleDetailIds.Add(finish.FinishProcessDetailId);
            }
            
            // 他サイクルのProcessDetailを取得
            if (otherCycleDetailIds.Count > 0)
            {
                var otherCycleDetails = _repository.GetProcessDetails()
                    .Where(d => otherCycleDetailIds.Contains(d.Id))
                    .ToList();
                
                // 他サイクルのProcess情報も取得
                var otherCycleCycleIds = otherCycleDetails.Select(d => d.CycleId).Distinct().ToList();
                var otherCycleProcesses = _repository.GetProcesses()
                    .Where(p => otherCycleCycleIds.Contains(p.CycleId))
                    .ToList();
                
                // 他サイクルの工程マップを更新
                foreach (var process in otherCycleProcesses)
                {
                    if (!processMap.ContainsKey(process.Id))
                    {
                        processMap[process.Id] = process.ProcessName ?? $"工程{process.Id}";
                    }
                }
                
                // 他サイクルのノードを作成
                double otherCycleY = 50;
                foreach (var detail in otherCycleDetails.OrderBy(d => d.CycleId).ThenBy(d => d.SortNumber))
                {
                    var position = new Point(1400, otherCycleY); // 右側に配置
                    var node = CreateNode(detail, position, categoriesList, processMap);
                    node.IsOtherCycleNode = true;
                    
                    nodeDict[detail.Id] = node;
                    AllNodes.Add(node);
                    Nodes.Add(node);
                    
                    otherCycleY += 80;
                }
            }
        }
        
        private Dictionary<int, int> CalculateNodeLevels(List<ProcessDetail> details)
        {
            var levels = new Dictionary<int, int>();
            var connections = _dbConnections.Where(c => 
                details.Any(d => d.Id == c.FromProcessDetailId) && 
                details.Any(d => d.Id == c.ToProcessDetailId)).ToList();
            
            // 初期レベルを設定
            foreach (var detail in details)
            {
                levels[detail.Id] = 0;
            }
            
            // 接続に基づいてレベルを計算
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var conn in connections)
                {
                    if (levels.ContainsKey(conn.FromProcessDetailId) && levels.ContainsKey(conn.ToProcessDetailId))
                    {
                        int newLevel = levels[conn.FromProcessDetailId] + 1;
                        if (levels[conn.ToProcessDetailId] < newLevel)
                        {
                            levels[conn.ToProcessDetailId] = newLevel;
                            changed = true;
                        }
                    }
                }
            }
            
            return levels;
        }
        
        private void CreateConnections(Dictionary<int, ProcessFlowNode> nodeDict)
        {
            // 通常の接続を作成
            foreach (var conn in _dbConnections)
            {
                if (nodeDict.ContainsKey(conn.FromProcessDetailId) && nodeDict.ContainsKey(conn.ToProcessDetailId))
                {
                    var fromNode = nodeDict[conn.FromProcessDetailId];
                    var toNode = nodeDict[conn.ToProcessDetailId];
                    
                    var connection = new ProcessFlowConnection(fromNode, toNode)
                    {
                        IsFinishConnection = false,
                        IsOtherCycleConnection = fromNode.ProcessDetail.CycleId != _cycleId || toNode.ProcessDetail.CycleId != _cycleId
                    };
                    connection.DbStartSensor = conn.StartSensor ?? "";
                    
                    AllConnections.Add(connection);
                    Connections.Add(connection);
                }
            }
            
            // 終了条件接続を作成
            foreach (var finish in _dbFinishes)
            {
                if (nodeDict.ContainsKey(finish.ProcessDetailId) && nodeDict.ContainsKey(finish.FinishProcessDetailId))
                {
                    var fromNode = nodeDict[finish.ProcessDetailId];
                    var toNode = nodeDict[finish.FinishProcessDetailId];
                    
                    var connection = new ProcessFlowConnection(fromNode, toNode)
                    {
                        IsFinishConnection = true,
                        IsOtherCycleConnection = fromNode.ProcessDetail.CycleId != _cycleId || toNode.ProcessDetail.CycleId != _cycleId
                    };
                    connection.DbStartSensor = finish.FinishSensor ?? "";
                    
                    AllConnections.Add(connection);
                    Connections.Add(connection);
                }
            }
        }
        
        private void CreateCompositeGroups(Dictionary<int, ProcessFlowNode> nodeDict, List<Models.Process> processes)
        {
            // 複合工程のグループ化は現在使用していないためコメントアウト
            // 必要に応じて後で実装
        }
        
        partial void OnSelectedNodeChanged(ProcessFlowNode? value)
        {
            if (value != null)
            {
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
                SelectedNodeProcessId = value.ProcessDetail.ProcessId;
                SelectedNodeStartTimerId = value.ProcessDetail.StartTimerId;
                SelectedNodeDisplayName = value.DisplayName;
                IsNodeSelected = true;
                
                // 接続情報を更新
                UpdateNodeConnections(value);
            }
            else
            {
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
                SelectedNodeProcessId = null;
                SelectedNodeStartTimerId = null;
                SelectedNodeDisplayName = "";
                IsNodeSelected = false;
                
                IncomingConnections.Clear();
                OutgoingConnections.Clear();
                HasOtherCycleConnections = false;
            }
            
            // 全ノードの選択状態を更新
            foreach (var node in Nodes)
            {
                node.IsSelected = node == value;
            }
        }
        
        private void UpdateNodeConnections(ProcessFlowNode node)
        {
            IncomingConnections.Clear();
            OutgoingConnections.Clear();
            HasOtherCycleConnections = false;
            
            // 接続元（このノードへの接続）
            var incoming = AllConnections.Where(c => c.ToNode == node).ToList();
            foreach (var conn in incoming)
            {
                IncomingConnections.Add(conn);
                if (conn.IsOtherCycleConnection)
                    HasOtherCycleConnections = true;
            }
            
            // 接続先（このノードからの接続）
            var outgoing = AllConnections.Where(c => c.FromNode == node).ToList();
            foreach (var conn in outgoing)
            {
                OutgoingConnections.Add(conn);
                if (conn.IsOtherCycleConnection)
                    HasOtherCycleConnections = true;
            }
        }
        
        [RelayCommand]
        private void CanvasMouseDown(MouseButtonEventArgs e)
        {
            if (e.Source is Canvas canvas)
            {
                var position = e.GetPosition(canvas);
                MousePosition = position;
                
                // Ctrlキーが押されている場合は接続モード
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    return;
                }
                
                // 左クリックで選択開始
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _isSelecting = true;
                    _selectionStartPoint = position;
                    _hasMouseMoved = false;
                    IsRectangleSelecting = false;
                    SelectionRectangle = new Rect(-1, -1, 0, 0);
                    
                    // すべてのノードの選択を解除
                    SelectedNode = null;
                    foreach (var node in Nodes)
                    {
                        node.IsSelected = false;
                    }
                    _selectedNodes.Clear();
                    
                    // 接続の選択も解除
                    SelectedConnection = null;
                    foreach (var conn in Connections)
                    {
                        conn.IsSelected = false;
                    }
                }
            }
        }
        
        [RelayCommand]
        private void CanvasMouseMove(MouseEventArgs e)
        {
            if (e.Source is Canvas canvas)
            {
                var position = e.GetPosition(canvas);
                MousePosition = position;
                
                if (IsConnecting && _connectionStartNode != null)
                {
                    // 接続線の更新はバインディングで自動的に行われる
                }
                else if (_draggedNode != null)
                {
                    // ドラッグ中の処理
                    if (_selectedNodes.Count > 1 && _selectedNodes.Contains(_draggedNode))
                    {
                        // 複数選択されている場合、すべて移動
                        foreach (var node in _selectedNodes)
                        {
                            if (_dragOffsets.ContainsKey(node))
                            {
                                var offset = _dragOffsets[node];
                                node.Position = new Point(position.X - offset.X, position.Y - offset.Y);
                            }
                        }
                    }
                    else
                    {
                        // 単一ノードの移動
                        _draggedNode.Position = new Point(position.X - _dragOffset.X, position.Y - _dragOffset.Y);
                    }
                    
                    // 接続線の更新は自動的に処理される
                }
                else if (_isSelecting && e.LeftButton == MouseButtonState.Pressed)
                {
                    // 矩形選択中
                    if (!_hasMouseMoved)
                    {
                        var distance = Math.Sqrt(Math.Pow(position.X - _selectionStartPoint.X, 2) + 
                                                Math.Pow(position.Y - _selectionStartPoint.Y, 2));
                        if (distance > 5) // 5ピクセル以上動いたら選択開始
                        {
                            _hasMouseMoved = true;
                            IsRectangleSelecting = true;
                        }
                    }
                    
                    if (_hasMouseMoved)
                    {
                        var x = Math.Min(_selectionStartPoint.X, position.X);
                        var y = Math.Min(_selectionStartPoint.Y, position.Y);
                        var width = Math.Abs(position.X - _selectionStartPoint.X);
                        var height = Math.Abs(position.Y - _selectionStartPoint.Y);
                        
                        SelectionRectangle = new Rect(x, y, width, height);
                        
                        // 矩形内のノードを選択
                        _selectedNodes.Clear();
                        foreach (var node in Nodes)
                        {
                            var nodeRect = new Rect(node.Position.X, node.Position.Y, 140, 40);
                            node.IsSelected = SelectionRectangle.IntersectsWith(nodeRect);
                            if (node.IsSelected)
                            {
                                _selectedNodes.Add(node);
                            }
                        }
                    }
                }
            }
        }
        
        [RelayCommand]
        private void CanvasMouseUp()
        {
            _draggedNode = null;
            _dragOffsets.Clear();
            IsConnecting = false;
            _connectionStartNode = null;
            _isSelecting = false;
            _hasMouseMoved = false;
            IsRectangleSelecting = false;
            SelectionRectangle = new Rect(-1, -1, 0, 0);
            
            // ドラッグ状態をリセット
            foreach (var node in Nodes)
            {
                node.IsDragging = false;
            }
        }
        
        [RelayCommand]
        private void NodeMouseDown(ProcessFlowNode node)
        {
            if (node == null) return;
            
            var position = MousePosition;
            
            // Ctrlキーが押されている場合は接続を作成
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (_connectionStartNode == null)
                {
                    _connectionStartNode = node;
                    IsConnecting = true;
                    _isCreatingFinishConnection = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    ConnectionStartPoint = new Point(node.Position.X + 70, node.Position.Y + 20);
                    IsConnecting = true;
                }
                return;
            }
            
            // Shiftキーが押されている場合は複数選択
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                node.IsSelected = !node.IsSelected;
                if (node.IsSelected)
                {
                    if (!_selectedNodes.Contains(node))
                        _selectedNodes.Add(node);
                }
                else
                {
                    _selectedNodes.Remove(node);
                }
                return;
            }
            
            // 通常のクリックの場合
            if (!node.IsSelected)
            {
                // 他の選択を解除
                foreach (var n in Nodes)
                {
                    n.IsSelected = false;
                }
                _selectedNodes.Clear();
                
                // このノードを選択
                node.IsSelected = true;
                SelectedNode = node;
                _selectedNodes.Add(node);
            }
            
            // ドラッグ開始
            _draggedNode = node;
            _dragOffset = new Point(position.X - node.Position.X, position.Y - node.Position.Y);
            node.IsDragging = true;
            
            // 複数選択されている場合、各ノードのオフセットを記録
            if (_selectedNodes.Count > 1)
            {
                _dragOffsets.Clear();
                foreach (var n in _selectedNodes)
                {
                    _dragOffsets[n] = new Point(position.X - n.Position.X, position.Y - n.Position.Y);
                    n.IsDragging = true;
                }
            }
        }
        
        [RelayCommand]
        private void NodeMouseUp(ProcessFlowNode node)
        {
            if (node == null) return;
            
            // 接続を完成させる
            if (_isConnecting && _connectionStartNode != null && _connectionStartNode != node)
            {
                // 同じ接続が既に存在するかチェック
                var existingConnection = Connections.FirstOrDefault(c =>
                    c.FromNode == _connectionStartNode && c.ToNode == node &&
                    c.IsFinishConnection == _isCreatingFinishConnection);
                
                if (existingConnection == null)
                {
                    var newConnection = new ProcessFlowConnection(_connectionStartNode, node)
                    {
                        IsFinishConnection = _isCreatingFinishConnection,
                        IsOtherCycleConnection = _connectionStartNode.ProcessDetail.CycleId != _cycleId || 
                                                node.ProcessDetail.CycleId != _cycleId
                    };
                    
                    AllConnections.Add(newConnection);
                    Connections.Add(newConnection);
                    
                    // データベースに保存
                    if (_isCreatingFinishConnection)
                    {
                        var dbFinish = new ProcessDetailFinish
                        {
                            ProcessDetailId = _connectionStartNode.ProcessDetail.Id,
                            FinishProcessDetailId = node.ProcessDetail.Id,
                            FinishSensor = ""
                        };
                        _repository.AddProcessDetailFinish(dbFinish);
                        _dbFinishes.Add(dbFinish);
                    }
                    else
                    {
                        var dbConnection = new ProcessDetailConnection
                        {
                            FromProcessDetailId = _connectionStartNode.ProcessDetail.Id,
                            ToProcessDetailId = node.ProcessDetail.Id,
                            StartSensor = ""
                        };
                        _repository.AddProcessDetailConnection(dbConnection);
                        _dbConnections.Add(dbConnection);
                    }
                }
            }
            
            IsConnecting = false;
            _connectionStartNode = null;
            IsConnecting = false;
        }
        
        [RelayCommand]
        private void EditSelectedNode()
        {
            // ダブルクリックでプロパティウィンドウを開く/フォーカスする
            // (ProcessFlowDetailWindow.xaml.cs側で処理される)
            if (SelectedNode != null)
            {
                // PropertyChangedイベントを明示的に発火させる
                OnPropertyChanged(nameof(SelectedNode));
            }
        }
        
        [RelayCommand]
        private void SaveChanges()
        {
            try
            {
                // ProcessDetailの位置とプロパティを保存
                foreach (var node in AllNodes.Where(n => n.ProcessDetail.CycleId == _cycleId))
                {
                    // ProcessDetailの位置情報を更新
                    _repository.UpdateProcessDetail(node.ProcessDetail);
                    node.IsModified = false;
                }
                
                // 接続の変更を保存
                foreach (var connection in AllConnections.Where(c => c.IsModified))
                {
                    if (connection.IsFinishConnection)
                    {
                        var finish = _dbFinishes.FirstOrDefault(f =>
                            f.ProcessDetailId == connection.FromNode.ProcessDetail.Id &&
                            f.FinishProcessDetailId == connection.ToNode.ProcessDetail.Id);
                        
                        if (finish != null)
                        {
                            finish.FinishSensor = connection.StartSensor;
                            // UpdateProcessDetailFinishメソッドが存在しない場合は削除と追加で対応
                            _repository.DeleteProcessDetailFinish(finish.Id);
                            _repository.AddProcessDetailFinish(finish);
                        }
                    }
                    else
                    {
                        var conn = _dbConnections.FirstOrDefault(c =>
                            c.FromProcessDetailId == connection.FromNode.ProcessDetail.Id &&
                            c.ToProcessDetailId == connection.ToNode.ProcessDetail.Id);
                        
                        if (conn != null)
                        {
                            conn.StartSensor = connection.StartSensor;
                            // UpdateProcessDetailConnectionメソッドが存在しない場合は削除と追加で対応
                            _repository.DeleteProcessDetailConnection(conn.Id);
                            _repository.AddProcessDetailConnection(conn);
                        }
                    }
                    
                    connection.IsModified = false;
                }
                
                MessageBox.Show("変更を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        [RelayCommand]
        private void AddNewNode()
        {
            // 新しいノードを追加するロジック
            var newDetail = new ProcessDetail
            {
                CycleId = _cycleId,
                DetailName = "新規工程",
                SortNumber = AllNodes.Count(n => n.ProcessDetail.CycleId == _cycleId) + 1
            };
            
            // データベースに追加
            _repository.AddProcessDetail(newDetail);
            
            // UIに追加
            var position = new Point(100, 100);
            var node = new ProcessFlowNode(newDetail, position);
            AllNodes.Add(node);
            Nodes.Add(node);
            
            SelectedNode = node;
        }
        
        [RelayCommand]
        private void DeleteSelectedNode()
        {
            if (SelectedNode == null || SelectedNode.ProcessDetail.CycleId != _cycleId) return;
            
            var result = MessageBox.Show($"選択したノード「{SelectedNode.DisplayName}」を削除しますか？", 
                "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // 関連する接続を削除
                var connectionsToRemove = AllConnections
                    .Where(c => c.FromNode == SelectedNode || c.ToNode == SelectedNode)
                    .ToList();
                
                foreach (var conn in connectionsToRemove)
                {
                    AllConnections.Remove(conn);
                    Connections.Remove(conn);
                    
                    // データベースから削除
                    if (conn.IsFinishConnection)
                    {
                        var finish = _dbFinishes.FirstOrDefault(f =>
                            f.ProcessDetailId == conn.FromNode.ProcessDetail.Id &&
                            f.FinishProcessDetailId == conn.ToNode.ProcessDetail.Id);
                        if (finish != null)
                            _repository.DeleteProcessDetailFinish(finish.Id);
                    }
                    else
                    {
                        var connection = _dbConnections.FirstOrDefault(c =>
                            c.FromProcessDetailId == conn.FromNode.ProcessDetail.Id &&
                            c.ToProcessDetailId == conn.ToNode.ProcessDetail.Id);
                        if (connection != null)
                            _repository.DeleteProcessDetailConnection(connection.Id);
                    }
                }
                
                // ノードを削除
                AllNodes.Remove(SelectedNode);
                Nodes.Remove(SelectedNode);
                
                // データベースから削除
                _repository.DeleteProcessDetail(SelectedNode.ProcessDetail.Id);
                
                SelectedNode = null;
            }
        }
        
        [RelayCommand]
        private void SelectConnection(ProcessFlowConnection connection)
        {
            if (connection == null) return;
            
            // 他の接続の選択を解除
            foreach (var conn in Connections)
            {
                conn.IsSelected = false;
            }
            
            // この接続を選択
            connection.IsSelected = true;
            SelectedConnection = connection;
            
            // 接続情報ウィンドウを表示するイベントを発生させる
            ConnectionSelected?.Invoke(this, EventArgs.Empty);
        }
        
        [RelayCommand]
        private void DeleteConnection(ProcessFlowConnection connection)
        {
            if (connection == null) return;
            
            var result = MessageBox.Show("選択した接続を削除しますか？", 
                "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                AllConnections.Remove(connection);
                Connections.Remove(connection);
                
                // データベースから削除
                if (connection.IsFinishConnection)
                {
                    var finish = _dbFinishes.FirstOrDefault(f =>
                        f.ProcessDetailId == connection.FromNode.ProcessDetail.Id &&
                        f.FinishProcessDetailId == connection.ToNode.ProcessDetail.Id);
                    if (finish != null)
                    {
                        _repository.DeleteProcessDetailFinish(finish.Id);
                        _dbFinishes.Remove(finish);
                    }
                }
                else
                {
                    var conn = _dbConnections.FirstOrDefault(c =>
                        c.FromProcessDetailId == connection.FromNode.ProcessDetail.Id &&
                        c.ToProcessDetailId == connection.ToNode.ProcessDetail.Id);
                    if (conn != null)
                    {
                        _repository.DeleteProcessDetailConnection(conn.Id);
                        _dbConnections.Remove(conn);
                    }
                }
                
                SelectedConnection = null;
            }
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
        private void Close()
        {
            // ウィンドウを閉じるためのイベントを発行
            RequestClose?.Invoke();
        }
        
        public event Action? RequestClose;
        
        [RelayCommand]
        private void FilterBySelectedNode()
        {
            if (SelectedNode == null) return;
            
            FilterNode = SelectedNode;
            IsFiltered = true;
            
            // 関連するノードを見つける
            var relatedNodeIds = new HashSet<int> { SelectedNode.ProcessDetail.Id };
            
            // 接続元と接続先を再帰的に追加
            bool added = true;
            while (added)
            {
                added = false;
                var currentIds = relatedNodeIds.ToList();
                foreach (var id in currentIds)
                {
                    var node = AllNodes.FirstOrDefault(n => n.ProcessDetail.Id == id);
                    if (node != null)
                    {
                        // このノードへの接続
                        var incoming = AllConnections.Where(c => c.ToNode == node).Select(c => c.FromNode.ProcessDetail.Id);
                        // このノードからの接続
                        var outgoing = AllConnections.Where(c => c.FromNode == node).Select(c => c.ToNode.ProcessDetail.Id);
                        
                        foreach (var relatedId in incoming.Concat(outgoing))
                        {
                            if (relatedNodeIds.Add(relatedId))
                            {
                                added = true;
                            }
                        }
                    }
                }
            }
            
            // フィルタリング
            Nodes.Clear();
            Connections.Clear();
            
            foreach (var node in AllNodes.Where(n => relatedNodeIds.Contains(n.ProcessDetail.Id)))
            {
                Nodes.Add(node);
            }
            
            foreach (var conn in AllConnections.Where(c => 
                relatedNodeIds.Contains(c.FromNode.ProcessDetail.Id) && 
                relatedNodeIds.Contains(c.ToNode.ProcessDetail.Id)))
            {
                Connections.Add(conn);
            }
        }
        
        [RelayCommand]
        private void FilterByDirectNeighbors()
        {
            if (SelectedNode == null) return;
            
            FilterNode = SelectedNode;
            IsFiltered = true;
            
            // 直接の前後のノードのみを表示
            var relatedNodeIds = new HashSet<int> { SelectedNode.ProcessDetail.Id };
            
            // 直接の接続元
            var incoming = AllConnections.Where(c => c.ToNode == SelectedNode).Select(c => c.FromNode.ProcessDetail.Id);
            // 直接の接続先
            var outgoing = AllConnections.Where(c => c.FromNode == SelectedNode).Select(c => c.ToNode.ProcessDetail.Id);
            
            foreach (var id in incoming.Concat(outgoing))
            {
                relatedNodeIds.Add(id);
            }
            
            // フィルタリング
            Nodes.Clear();
            Connections.Clear();
            
            foreach (var node in AllNodes.Where(n => relatedNodeIds.Contains(n.ProcessDetail.Id)))
            {
                Nodes.Add(node);
            }
            
            foreach (var conn in AllConnections.Where(c => 
                relatedNodeIds.Contains(c.FromNode.ProcessDetail.Id) && 
                relatedNodeIds.Contains(c.ToNode.ProcessDetail.Id)))
            {
                Connections.Add(conn);
            }
        }
        
        [RelayCommand]
        private void ResetFilter()
        {
            IsFiltered = false;
            FilterNode = null;
            
            // すべてのノードと接続を表示
            Nodes.Clear();
            Connections.Clear();
            
            foreach (var node in AllNodes)
            {
                Nodes.Add(node);
            }
            
            foreach (var conn in AllConnections)
            {
                Connections.Add(conn);
            }
        }
        
        [RelayCommand]
        private void EditOperation()
        {
            if (SelectedNode == null) return;
            
            // Operation編集画面を開く（実装は別途必要）
            MessageBox.Show($"Operation編集機能は未実装です。\nノード: {SelectedNode.DisplayName}", 
                "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        partial void OnShowAllConnectionsChanged(bool value)
        {
            // 再読み込み
            LoadProcessDetails();
        }
        
        // ズーム機能のメソッド
        public void ZoomIn()
        {
            var newScale = Math.Min(ZoomScale + ZoomStep, MaxZoomScale);
            ZoomScale = Math.Round(newScale, 2);
        }
        
        public void ZoomOut()
        {
            var newScale = Math.Max(ZoomScale - ZoomStep, MinZoomScale);
            ZoomScale = Math.Round(newScale, 2);
        }
        
        public void SetZoom(double scale)
        {
            ZoomScale = Math.Round(Math.Max(MinZoomScale, Math.Min(MaxZoomScale, scale)), 2);
        }
        
        [RelayCommand]
        private void ResetZoom()
        {
            ZoomScale = 1.0;
        }
    }
}