using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KdxDesigner.Models;
using KdxDesigner.Services.Access;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
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
        
        [ObservableProperty] private ObservableCollection<ProcessFlowNode> _nodes = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _connections = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowNode> _allNodes = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _allConnections = new();
        [ObservableProperty] private ProcessFlowNode? _selectedNode;
        [ObservableProperty] private int _cycleId;
        [ObservableProperty] private string _selectedNodeStartIds = "";
        [ObservableProperty] private string _selectedNodeStartSensor = "";
        [ObservableProperty] private Point _mousePosition;
        [ObservableProperty] private bool _isConnecting;
        [ObservableProperty] private Point _connectionStartPoint;
        [ObservableProperty] private bool _isFiltered = false;
        [ObservableProperty] private ProcessFlowNode? _filterNode;
        [ObservableProperty] private double _canvasWidth = 2000;
        [ObservableProperty] private double _canvasHeight = 2000;
        
        public ProcessFlowViewModel(IAccessRepository repository)
        {
            _repository = repository;
        }
        
        public void LoadProcessDetails(int cycleId)
        {
            CycleId = cycleId;
            var details = _repository.GetProcessDetails()
                .Where(d => d.CycleId == cycleId)
                .OrderBy(d => d.SortNumber)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"Loading {details.Count} ProcessDetails for CycleId: {cycleId}");
            
            AllNodes.Clear();
            AllConnections.Clear();
            Nodes.Clear();
            Connections.Clear();
            
            // ノードを作成し、レイアウトを計算
            var nodeDict = new Dictionary<int, ProcessFlowNode>();
            var levels = CalculateNodeLevels(details);
            
            // レベルごとのノード数をカウント
            var levelCounts = new Dictionary<int, int>();
            foreach (var detail in details)
            {
                var level = levels.ContainsKey(detail.Id) ? levels[detail.Id] : 0;
                if (!levelCounts.ContainsKey(level))
                    levelCounts[level] = 0;
                levelCounts[level]++;
            }
            
            // 各レベルの現在のノード数を追跡
            var currentLevelCounts = new Dictionary<int, int>();
            double maxX = 0, maxY = 0;
            
            foreach (var detail in details)
            {
                var level = levels.ContainsKey(detail.Id) ? levels[detail.Id] : 0;
                if (!currentLevelCounts.ContainsKey(level))
                    currentLevelCounts[level] = 0;
                
                var position = new Point(
                    level * 150 + 50,  // 水平間隔を250から150に縮小
                    currentLevelCounts[level] * 60 + 50  // 垂直間隔を100から60に縮小
                );
                currentLevelCounts[level]++;
                
                // Canvas サイズ計算用の最大値を追跡
                maxX = Math.Max(maxX, position.X + 120); // ノード幅を加算
                maxY = Math.Max(maxY, position.Y + 40);  // ノード高さを加算
                
                var node = new ProcessFlowNode(detail, position);
                AllNodes.Add(node);
                Nodes.Add(node);
                nodeDict[detail.Id] = node;
                
                System.Diagnostics.Debug.WriteLine($"Added node: {node.DisplayName} at position ({position.X}, {position.Y})");
            }
            
            // Canvasサイズを設定（余白を追加）
            CanvasWidth = Math.Max(2000, maxX + 100);
            CanvasHeight = Math.Max(2000, maxY + 100);
            
            // コネクションを作成
            foreach (var detail in details)
            {
                if (!string.IsNullOrEmpty(detail.StartIds))
                {
                    var startIds = detail.StartIds.Split(';')
                        .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value);
                    
                    foreach (var startId in startIds)
                    {
                        if (nodeDict.ContainsKey(startId) && nodeDict.ContainsKey(detail.Id))
                        {
                            var connection = new ProcessFlowConnection(
                                nodeDict[startId],
                                nodeDict[detail.Id]
                            );
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
            
            // StartIdsが空のノードはレベル0
            foreach (var detail in details.Where(d => string.IsNullOrEmpty(d.StartIds)))
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
                    if (!string.IsNullOrEmpty(detail.StartIds))
                    {
                        var startIds = detail.StartIds.Split(';')
                            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
                            .Where(id => id.HasValue)
                            .Select(id => id.Value)
                            .ToList();
                        
                        if (startIds.All(id => levels.ContainsKey(id)))
                        {
                            levels[detail.Id] = startIds.Max(id => levels[id]) + 1;
                            processed.Add(detail.Id);
                            changed = true;
                        }
                    }
                }
            }
            
            return levels;
        }
        
        partial void OnSelectedNodeChanged(ProcessFlowNode? value)
        {
            if (value != null)
            {
                SelectedNodeStartIds = value.StartIds;
                SelectedNodeStartSensor = value.StartSensor;
            }
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
                _dragOffset = new Point(
                    MousePosition.X - node.Position.X,
                    MousePosition.Y - node.Position.Y
                );
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
                
                if (_draggedNode != null)
                {
                    _draggedNode.Position = new Point(
                        position.X - _dragOffset.X,
                        position.Y - _dragOffset.Y
                    );
                    
                    // 関連するコネクションを更新
                    foreach (var connection in Connections)
                    {
                        if (connection.FromNode == _draggedNode || connection.ToNode == _draggedNode)
                        {
                            // プロパティ変更通知を強制的に発行
                            OnPropertyChanged(nameof(Connections));
                        }
                    }
                }
            }
        }
        
        [RelayCommand]
        private void CanvasMouseUp()
        {
            _draggedNode = null;
            EndConnection();
        }
        
        private void StartConnection(ProcessFlowNode node)
        {
            _connectionStartNode = node;
            _isCreatingConnection = true;
            IsConnecting = true;
            ConnectionStartPoint = new Point(
                node.Position.X + 60,
                node.Position.Y + 20
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
                
                // StartIdsを更新
                var currentIds = to.ProcessDetail.StartIds?.Split(';')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList() ?? new List<string>();
                
                if (!currentIds.Contains(from.Id.ToString()))
                {
                    currentIds.Add(from.Id.ToString());
                    to.ProcessDetail.StartIds = string.Join(";", currentIds);
                    
                    if (SelectedNode == to)
                    {
                        SelectedNodeStartIds = to.ProcessDetail.StartIds;
                    }
                }
            }
        }
        
        [RelayCommand]
        private void DeleteConnection(ProcessFlowConnection connection)
        {
            AllConnections.Remove(connection);
            Connections.Remove(connection);
            
            // StartIdsを更新
            var toNode = connection.ToNode;
            var fromId = connection.FromNode.Id.ToString();
            
            var currentIds = toNode.ProcessDetail.StartIds?.Split(';')
                .Where(s => !string.IsNullOrWhiteSpace(s) && s != fromId)
                .ToList() ?? new List<string>();
            
            toNode.ProcessDetail.StartIds = currentIds.Any() ? string.Join(";", currentIds) : "";
            
            // 削除によってtoNodeに接続されている他の接続も変更済みとしてマーク
            foreach (var conn in AllConnections.Where(c => c.ToNode == toNode))
            {
                conn.IsModified = true;
            }
            
            if (SelectedNode == toNode)
            {
                SelectedNodeStartIds = toNode.ProcessDetail.StartIds;
            }
        }
        
        [RelayCommand]
        private void UpdateSelectedNodeStartIds()
        {
            if (SelectedNode != null)
            {
                SelectedNode.ProcessDetail.StartIds = SelectedNodeStartIds;
                RefreshConnections();
                
                // このノードに関連するすべての接続を変更済みとしてマーク
                foreach (var conn in AllConnections.Where(c => c.ToNode == SelectedNode))
                {
                    conn.IsModified = true;
                }
            }
        }
        
        [RelayCommand]
        private void UpdateSelectedNodeStartSensor()
        {
            if (SelectedNode != null)
            {
                SelectedNode.ProcessDetail.StartSensor = SelectedNodeStartSensor;
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
            
            // StartIdsに基づいて新しい接続を作成
            if (!string.IsNullOrEmpty(SelectedNode.StartIds))
            {
                var startIds = SelectedNode.StartIds.Split(';')
                    .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value);
                
                foreach (var startId in startIds)
                {
                    var fromNode = Nodes.FirstOrDefault(n => n.Id == startId);
                    if (fromNode != null)
                    {
                        var connection = new ProcessFlowConnection(fromNode, SelectedNode);
                        Connections.Add(connection);
                    }
                }
            }
        }
        
        [RelayCommand]
        private void FilterBySelectedNode()
        {
            if (SelectedNode == null) return;
            
            var relatedNodes = GetRelatedNodes(SelectedNode);
            var relatedNodeIds = new HashSet<int>(relatedNodes.Select(n => n.Id));
            
            // ノードをフィルタリング
            Nodes.Clear();
            double maxX = 0, maxY = 0;
            foreach (var node in AllNodes.Where(n => relatedNodeIds.Contains(n.Id)))
            {
                Nodes.Add(node);
                maxX = Math.Max(maxX, node.Position.X + 120);
                maxY = Math.Max(maxY, node.Position.Y + 40);
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
        private void ResetFilter()
        {
            // すべてのノードとコネクションを表示
            Nodes.Clear();
            double maxX = 0, maxY = 0;
            foreach (var node in AllNodes)
            {
                Nodes.Add(node);
                maxX = Math.Max(maxX, node.Position.X + 120);
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
        
        private void GetPredecessors(ProcessFlowNode node, HashSet<ProcessFlowNode> visited)
        {
            // StartIdsから前のノードを取得
            if (!string.IsNullOrEmpty(node.StartIds))
            {
                var startIds = node.StartIds.Split(';')
                    .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value);
                
                foreach (var startId in startIds)
                {
                    var predecessorNode = AllNodes.FirstOrDefault(n => n.Id == startId);
                    if (predecessorNode != null && visited.Add(predecessorNode))
                    {
                        GetPredecessors(predecessorNode, visited);
                    }
                }
            }
        }
        
        private void GetSuccessors(ProcessFlowNode node, HashSet<ProcessFlowNode> visited)
        {
            // このノードに依存している後続ノードを取得
            var successors = AllNodes.Where(n => 
            {
                if (string.IsNullOrEmpty(n.StartIds)) return false;
                
                var startIds = n.StartIds.Split(';')
                    .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value);
                
                return startIds.Contains(node.Id);
            });
            
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
    }
}