// TeamExplorerPad.cs
// 
// Author:
//       Ventsislav Mladenov
// 
// The MIT License (MIT)
// 
// Copyright (c) 2013-2015 Ventsislav Mladenov
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using Autofac;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.VersionControl.TFS.Commands;
using MonoDevelop.VersionControl.TFS.Core.Structure;
using MonoDevelop.VersionControl.TFS.GUI.VersionControl;
using MonoDevelop.VersionControl.TFS.GUI.WorkItems;
using MonoDevelop.VersionControl.TFS.Infrastructure;
using MonoDevelop.VersionControl.TFS.WorkItemTracking.Structure;
using Gtk;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.GUI
{
    public class TeamExplorerPad : PadContent
    {
        private enum NodeType
        {
            Server,
            ProjectCollection,
            Project,
            SourceControl,
            WorkItems,
            WorkItemQueryType,
            WorkItemQuery,
            Exception
        }

        private readonly Xwt.VBox _content = new Xwt.VBox();
        private readonly Xwt.TreeView _treeView = new Xwt.TreeView();
        private readonly DataField<string> _name = new DataField<string>();
        private readonly DataField<NodeType> _type = new DataField<NodeType>();
        private readonly DataField<object> _item = new DataField<object>();
        private readonly Xwt.TreeStore _treeStore;
        private readonly System.Action onServersChanged;
        private readonly TFSVersionControlService _service;

        public TeamExplorerPad()
        {
            _treeStore = new Xwt.TreeStore(_name, _type, _item);
            _service = DependencyInjection.Container.Resolve<TFSVersionControlService>();
            onServersChanged = delegate ()
            {
                Runtime.RunInMainThread(() => UpdateData());
            
            };
            _service.OnServersChange += onServersChanged;
            BuildGui();
        }

        private void BuildGui()
        {
            _treeView.Columns.Add(new ListViewColumn(string.Empty, new TextCellView(_name)));
            _treeView.DataSource = _treeStore;
            _content.PackStart(_treeView, true, true);
            _treeView.RowActivated += OnRowClicked;
        }

        #region IPadContent implementation

        protected override void Initialize(IPadWindow window)
        {
            base.Initialize(window);
            //TODO: Add toolbar support
   //         DockItemToolbar toolBar = window.GetToolbar(Components.Docking.DockPositionType.Top);
   //         Button connectToServer = new Button(new ImageView(Ide.Gui.Stock.Stop, IconSize.Small));
			//buttonStop.Clicked += new EventHandler(OnStopClicked);
			//buttonStop.Sensitive = false;
			//buttonStop.TooltipText = GettextCatalog.GetString("Cancel running test");
			//buttonStop.Accessible.Name = "TestPad.StopAll";
			//buttonStop.Accessible.SetTitle(GettextCatalog.GetString(("Cancel")));
			//buttonStop.Accessible.Description = GettextCatalog.GetString("Stops the current test run");
			//topToolbar.Add(buttonStop);
			//topToolbar.ShowAll();
            //CommandToolButton button = new CommandToolButton(TFSCommands.ConnectToServer, IdeApp.CommandService) { StockId = Gtk.Stock.Add };
            //toolBar.Add(button);
            //UpdateData();
        }

        public void RedrawContent()
        {
            UpdateData();
        }

        public override Control Control
        {
            get
            {
                return (Gtk.Widget)Xwt.Toolkit.CurrentEngine.GetNativeWidget(this._content);
            }
        }

        #endregion

        #region IDisposable implementation

        public override void Dispose()
        {
            _service.OnServersChange -= onServersChanged;
            _treeView.Dispose();
            _treeStore.Dispose();
            _content.Dispose();
            base.Dispose();
        }

        #endregion

        private void UpdateData()
        {
            _treeStore.Clear();
            foreach (var server in _service.Servers)
            {
                var node = _treeStore.AddNode().SetValue(_name, server.Name)
                                               .SetValue(_type, NodeType.Server)
                                               .SetValue(_item, server);
                try
                {
                    foreach (var pc in server.ProjectCollections)
                    {
                        node.AddChild().SetValue(_name, pc.Name)
                                       .SetValue(_type, NodeType.ProjectCollection)
                                       .SetValue(_item, pc);

//                        var workItemManager = new WorkItemManager(pc);
                        foreach (var projectInfo in pc.Projects.OrderBy(x => x.Name))
                        {
                            node.AddChild().SetValue(_name, projectInfo.Name).SetValue(_type, NodeType.Project).SetValue(_item, projectInfo);
                            //TODO: How to be done not to call server when init the tree
//                            var workItemProject = workItemManager.GetByGuid(projectInfo.Guid);
//                            if (workItemProject != null)
//                            {
//                                node.AddChild().SetValue(_name, "Work Items").SetValue(_type, NodeType.WorkItems);
//                                var privateQueries = workItemManager.GetMyQueries(workItemProject);
//                                if (privateQueries.Any())
//                                {
//                                    node.AddChild().SetValue(_name, "My Queries").SetValue(_type, NodeType.WorkItemQueryType);
//                                    foreach (var query in privateQueries)
//                                    {
//                                        node.AddChild().SetValue(_name, query.QueryName).SetValue(_type, NodeType.WorkItemQuery).SetValue(_item, query);
//                                        node.MoveToParent();
//                                    }
//                                    node.MoveToParent();
//                                }
//                                var publicQueries = workItemManager.GetPublicQueries(workItemProject);
//                                if (publicQueries.Any())
//                                {
//                                    node.AddChild().SetValue(_name, "Public").SetValue(_type, NodeType.WorkItemQueryType);
//                                    foreach (var query in publicQueries)
//                                    {
//                                        node.AddChild().SetValue(_name, query.QueryName).SetValue(_type, NodeType.WorkItemQuery).SetValue(_item, query);
//                                        node.MoveToParent();
//                                    }
//                                    node.MoveToParent();
//                                }
//                                node.MoveToParent();
//                            }
                            node.AddChild().SetValue(_name, "Source Control").SetValue(_type, NodeType.SourceControl);
                            node.MoveToParent();
                            node.MoveToParent();
                        }
                        node.MoveToParent();
                    }
                }
                catch (Exception ex)
                {
                    node.AddChild().SetValue(_name, ex.Message).SetValue(_type, NodeType.Exception);
                }
            }
            ExpandServers();
        }

        private void ExpandServers()
        {
            var node = _treeStore.GetFirstNode();
            if (node.CurrentPosition == null)
                return;
            _treeView.ExpandRow(node.CurrentPosition, false);
            while (node.MoveNext())
            {
                _treeView.ExpandRow(node.CurrentPosition, false);
            }
        }

        #region Tree Events

        private void OnRowClicked(object sender, TreeViewRowEventArgs e)
        {
            var node = _treeStore.GetNavigatorAt(e.Position);
            var nodeType = node.GetValue(_type);
            ProjectInfo project;
            switch (nodeType)
            {
                case NodeType.SourceControl:
                    node.MoveToParent();
                    project = (ProjectInfo)node.GetValue(_item);
                    SourceControlExplorerView.Open(project);
                    break;
                case NodeType.WorkItemQuery:
                    var query = (StoredQuery)node.GetValue(_item);
                    node.MoveToParent(); //WorkItems
                    node.MoveToParent(); //Project
                    project = (ProjectInfo)node.GetValue(_item);
                    WorkItemsView.Open(query, project);
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
