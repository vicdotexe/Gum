﻿using System;
using System.Collections.Generic;
using System.Linq;
using CommonFormsAndControls;
using System.Windows.Forms;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.Variables;
using System.IO;
using ToolsUtilities;
using Gum.Events;
using Gum.Wireframe;
using Gum.DataTypes.Behaviors;
using Gum.Plugins;
using System.ComponentModel;
//using System.Windows.Controls;
//using System.Windows;
using Grid = System.Windows.Controls.Grid;
using Gum.Mvvm;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using RenderingLibrary.Graphics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using System.Management.Instrumentation;
using Gum.Logic;
using Gum.Controls;

namespace Gum.Managers
{
    #region ExpandedState class
    class ExpandedState
    {
        public bool ScreensExpanded { get; set; }
        public bool ComponentsExpanded { get; set; }
        public bool StandardsExpanded { get; set; }
        public bool BehaviorsExpanded { get; set; }

        public Dictionary<TreeNode, bool> ExpandedStates { get; set; } = new Dictionary<TreeNode, bool>();

        public void Record(TreeNode treeNode)
        {
            ExpandedStates[treeNode] = treeNode.IsExpanded;

            foreach(TreeNode subNode in treeNode.Nodes)
            {
                // we only care about directory nodes, as only those are going to be recorded.
                if (subNode.Nodes.Count > 0 && subNode.Tag == null)
                {
                    // record this bad boy:
                    Record(subNode);
                }
            }
        }
        public void Apply()
        {
            foreach(var kvp in ExpandedStates)
            {
                if(kvp.Value)
                {
                    kvp.Key.Expand();
                }
                else// if(nodesToKeepExpanded.Contains(kvp.Key) == false)
                {
                    kvp.Key.Collapse();
                }
            }
        }

        public void ExpandAll()
        {
            foreach(var kvp in ExpandedStates)
            {
                kvp.Key.Expand();
            }
        }
    }
    #endregion

    public partial class ElementTreeViewManager
    {
        #region Fields


        public const int TransparentImageIndex = 0;
        public const int FolderImageIndex = 1;
        public const int ComponentImageIndex = 2;
        public const int InstanceImageIndex = 3;
        public const int ScreenImageIndex = 4;
        public const int StandardElementImageIndex = 5;
        public const int ExclamationIndex = 6;
        public const int StateImageIndex = 7;
        public const int BehaviorImageIndex = 8;
        public const int DerivedInstanceImageIndex = 9;

        static ElementTreeViewManager mSelf;
        ContextMenuStrip mMenuStrip;
        

        MultiSelectTreeView ObjectTreeView;

        TreeNode mScreensTreeNode;
        TreeNode mComponentsTreeNode;
        TreeNode mStandardElementsTreeNode;
        TreeNode mBehaviorsTreeNode;

        FlatSearchListBox FlatList;
        System.Windows.Forms.Integration.WindowsFormsHost TreeViewHost;


        /// <summary>
        /// Used to store off what was previously selected
        /// when the tree view refreshes itself - so the user
        /// doesn't lose his selection.
        /// </summary>
        object mRecordedSelectedObject;

        TextBox searchTextBox;
        CheckBox deepSearchCheckBox;
        #endregion

        #region Properties

        public static ElementTreeViewManager Self
        {
            get 
            {
                if (mSelf == null)
                {
                    mSelf = new ElementTreeViewManager();
                }
                return mSelf; 
            }
        }

        public TreeNode SelectedNode
        {
            get
            {
                // This could be called before the tree is created:
                if (ObjectTreeView == null)
                {
                    return null;
                }
                else
                {
                    return ObjectTreeView.SelectedNode;
                }
            }
            set
            {
                ObjectTreeView.SelectedNode = value;
            }
        }

        public List<TreeNode> SelectedNodes
        {
            get
            {
                return ObjectTreeView.SelectedNodes;
            }
        }

        ExpandedState expandedStateBeforeFilter;
        string filterText;
        public string FilterText
        {
            get => filterText;
            set 
            {
                if(value != filterText)
                {
                    filterText = value;
                    ReactToFilterTextChanged();

                }
            }
        }


        private void SelectFirstElement()
        {
            TreeNode treeNode = 
                ObjectTreeView.Nodes.FirstOrDefault() as TreeNode;

            while(treeNode != null)
            {
                if (treeNode.Tag != null)
                {
                    Select(treeNode);
                    break;
                }
                else
                {
                    treeNode = treeNode.NextVisibleNode;
                }
            }
        }

        public TreeNode RootScreensTreeNode => mScreensTreeNode;

        public TreeNode RootComponentsTreeNode => mComponentsTreeNode;

        public TreeNode RootStandardElementsTreeNode => mStandardElementsTreeNode;

        public TreeNode RootBehaviorsTreeNode => mBehaviorsTreeNode;

        private CopyPasteLogic _copyPasteLogic;

        System.Windows.Forms.Cursor AddCursor { get; set; }

        public bool HasMouseOver
        {
            get
            {
                var mousePosition = Control.MousePosition;
                var clientPoint = ObjectTreeView.PointToClient(mousePosition);
                return ObjectTreeView.ClientRectangle.Contains(clientPoint);
            }
        }

        #endregion

        #region Methods


        #region Find/Get
        public TreeNode GetTreeNodeFor(ElementSave elementSave)
        {
            if (elementSave == null)
            {
                return null;
            }
            else if (elementSave is ScreenSave)
            {
                return GetTreeNodeFor(elementSave as ScreenSave);
            }
            else if (elementSave is ComponentSave)
            {
                return GetTreeNodeFor(elementSave as ComponentSave);
            }
            else if (elementSave is StandardElementSave)
            {
                return GetTreeNodeFor(elementSave as StandardElementSave);
            }

            return null;
        }




        public TreeNode GetTreeNodeFor(ScreenSave screenSave)
        {
            return GetTreeNodeForTag(screenSave, RootScreensTreeNode);
        }

        public TreeNode GetTreeNodeFor(ComponentSave componentSave)
        {
            return GetTreeNodeForTag(componentSave, RootComponentsTreeNode);
        }

        public TreeNode GetTreeNodeFor(StandardElementSave standardElementSave)
        {
            return GetTreeNodeForTag(standardElementSave, RootStandardElementsTreeNode);
        }

        public TreeNode GetTreeNodeFor(InstanceSave instanceSave, TreeNode container)
        {
            foreach (TreeNode node in container.Nodes)
            {
                if (node.Tag == instanceSave)
                {
                    return node;
                }

                TreeNode childNode = GetTreeNodeFor(instanceSave, node);
                if (childNode != null)
                {
                    return childNode;
                }
            }

            return null;
        }

        public TreeNode GetInstanceTreeNodeByName(string name, TreeNode container)
        {
            foreach (TreeNode node in container.Nodes)
            {
                if (node.Tag is InstanceSave instanceSave && instanceSave.Name == name)
                {
                    return node;
                }

                TreeNode childNode = GetInstanceTreeNodeByName(name, node);
                if (childNode != null)
                {
                    return childNode;
                }
            }

            return null;
        }

        public TreeNode GetTreeNodeFor(BehaviorSave behavior)
        {
            return GetTreeNodeForTag(behavior, RootBehaviorsTreeNode);
        }

        public TreeNode GetTreeNodeFor(string absoluteDirectory)
        {
            string relative = FileManager.MakeRelative(absoluteDirectory,
                FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName));


            relative = FileManager.Standardize(relative);
            // in the tool we use forward slashes:
            relative = relative.Replace("\\", "/");

            if (relative.StartsWith("screens/"))
            {
                string modifiedRelative = relative.Substring("screens/".Length);

                return GetTreeNodeFor(modifiedRelative, mScreensTreeNode);
            }
            else if (relative.StartsWith("components/"))
            {
                string modifiedRelative = relative.Substring("components/".Length);

                return GetTreeNodeFor(modifiedRelative, mComponentsTreeNode);
            }
            else if (relative.StartsWith("standards/"))
            {
                string modifiedRelative = relative.Substring("standards/".Length);

                return GetTreeNodeFor(modifiedRelative, mStandardElementsTreeNode);
            }
            else if(relative.StartsWith("behaviors/"))
            {
                string modifiedRelative = relative.Substring("behaviors/".Length);

                return GetTreeNodeFor(modifiedRelative, mBehaviorsTreeNode);
            }

            return null;

        }

        TreeNode GetTreeNodeFor(string relativeDirectory, TreeNode container)
        {
            if (string.IsNullOrEmpty(relativeDirectory))
            {
                return container;
            }

            int indexOfSlash = relativeDirectory.IndexOf('/');
            string whatToLookFor = relativeDirectory;
            string sub = "";

            if (indexOfSlash != -1)
            {
                whatToLookFor = relativeDirectory.Substring(0, indexOfSlash);
                sub = relativeDirectory.Substring(indexOfSlash + 1, relativeDirectory.Length - (indexOfSlash + 1));
            }

            foreach (TreeNode node in container.Nodes)
            {
                if (node.Text.Equals(whatToLookFor, StringComparison.OrdinalIgnoreCase))
                {
                    return GetTreeNodeFor(sub, node);
                }
            }

            return null;
        }

        TreeNode GetTreeNodeForTag(object tag, TreeNode container = null)
        {
            if (container == null)
            {
                if (tag is ScreenSave)
                {
                    container = RootScreensTreeNode;
                }
                else if (tag is ComponentSave)
                {
                    container = RootComponentsTreeNode;
                }
                else if (tag is StandardElementSave)
                {
                    container = RootStandardElementsTreeNode;
                }
                else if(tag is BehaviorSave)
                {
                    container = RootBehaviorsTreeNode;
                }
            }

            foreach (TreeNode treeNode in container.Nodes)
            {
                if (treeNode.Tag == tag)
                {
                    return treeNode;
                }

                var found = GetTreeNodeForTag(tag, treeNode);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public TreeNode GetTreeNodeOver()
        {
            System.Drawing.Point point = ObjectTreeView.PointToClient(Cursor.Position);

            return ObjectTreeView.GetNodeAt(point);
        }

        #endregion


        public void Initialize(IContainer components, ImageList ElementTreeImages,
            System.Windows.Forms.Cursor addCursor, CopyPasteLogic copyPasteLogic)
        {
            _copyPasteLogic = copyPasteLogic;

            AddCursor = addCursor;

            CreateObjectTreeView(ElementTreeImages);

            CreateContextMenuStrip(components);

            GumCommands.Self.GuiCommands.RefreshElementTreeView();

            InitializeMenuItems();

            //var panel = new Panel();

            var grid = new Grid();
            grid.RowDefinitions.Add(
                new System.Windows.Controls.RowDefinition() 
                { Height = new System.Windows.GridLength(22, System.Windows.GridUnitType.Pixel) });
            grid.RowDefinitions.Add(
                new System.Windows.Controls.RowDefinition()
                { Height = new System.Windows.GridLength(22, System.Windows.GridUnitType.Pixel) });
            grid.RowDefinitions.Add(
                new System.Windows.Controls.RowDefinition() 
                { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

            GumCommands.Self.GuiCommands.AddControl(grid, "Project", TabLocation.Left);

            ObjectTreeView.Dock = DockStyle.Fill;
            //panel.Controls.Add(ObjectTreeView);
            TreeViewHost = new System.Windows.Forms.Integration.WindowsFormsHost();
            TreeViewHost.Child = ObjectTreeView;
            Grid.SetRow(TreeViewHost, 2);
            grid.Children.Add(TreeViewHost);


            var searchBarUi = CreateSearchBoxUi();
            var searchBarHost = new System.Windows.Forms.Integration.WindowsFormsHost();
            searchBarHost.Child = searchBarUi;
            Grid.SetRow(searchBarHost, 0);
            grid.Children.Add(searchBarHost);

            var checkBoxUi = CreateSearchCheckBoxUi();
            var checkBoxHost = new System.Windows.Forms.Integration.WindowsFormsHost();
            checkBoxHost.Child = checkBoxUi;
            Grid.SetRow(checkBoxHost, 1);
            grid.Children.Add(checkBoxHost);

            FlatList = CreateFlatSearchList();
            FlatList.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            FlatList.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

            Grid.SetRow(FlatList, 2);
            grid.Children.Add(FlatList);



            //GumCommands.Self.GuiCommands.AddControl(panel, "Project", TabLocation.Left);
        }


        internal void FocusSearch()
        {
            searchTextBox.Focus();
        }

        private void CreateContextMenuStrip(IContainer components)
        {
            this.mMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
            this.mMenuStrip.Name = "ElementMenuStrip";
            this.mMenuStrip.Size = new System.Drawing.Size(61, 4);
            this.ObjectTreeView.ContextMenuStrip = this.mMenuStrip;
        }

        private void CreateObjectTreeView(ImageList ElementTreeImages)
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.ObjectTreeView = new CommonFormsAndControls.MultiSelectTreeView();
            this.ObjectTreeView.IsSelectingOnPush = false;
            this.ObjectTreeView.AllowDrop = true;
            this.ObjectTreeView.AlwaysHaveOneNodeSelected = false;
            this.ObjectTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ObjectTreeView.HotTracking = true;
            this.ObjectTreeView.ImageIndex = 0;
            this.ObjectTreeView.ImageList = ElementTreeImages;
            this.ObjectTreeView.Location = new System.Drawing.Point(0, 0);
            this.ObjectTreeView.MultiSelectBehavior = CommonFormsAndControls.MultiSelectBehavior.CtrlDown;
            this.ObjectTreeView.Name = "ObjectTreeView";
            this.ObjectTreeView.SelectedImageIndex = 0;
            this.ObjectTreeView.SelectedNodes = ((System.Collections.Generic.List<System.Windows.Forms.TreeNode>)(resources.GetObject("ObjectTreeView.SelectedNodes")));
            this.ObjectTreeView.Size = new System.Drawing.Size(196, 621);
            this.ObjectTreeView.TabIndex = 0;
            this.ObjectTreeView.AfterClickSelect += this.ObjectTreeView_AfterClickSelect;
            this.ObjectTreeView.AfterSelect += this.ObjectTreeView_AfterSelect_1;
            this.ObjectTreeView.KeyDown += this.ObjectTreeView_KeyDown;
            this.ObjectTreeView.KeyPress += this.ObjectTreeView_KeyPress;
            this.ObjectTreeView.PreviewKeyDown += this.ObjectTreeView_PreviewKeyDown;
            this.ObjectTreeView.MouseClick += this.ObjectTreeView_MouseClick;
            this.ObjectTreeView.MouseMove += (sender, e) => HandleMouseOver(e.X, e.Y);
            ObjectTreeView.DragDrop += HandleDragDropEvent;

            ObjectTreeView.ItemDrag += (sender, e) =>
            {
                DragDropManager.Self.OnItemDrag(e.Item);
                System.Diagnostics.Debug.WriteLine("ItemDrag");

                ObjectTreeView.DoDragDrop(e.Item, DragDropEffects.Move | DragDropEffects.Copy);
            };

            ObjectTreeView.DragEnter += (sender, e) =>
            {
                e.Effect = DragDropEffects.All;

            };

            ObjectTreeView.DragOver += (sender, e) =>
            {
                e.Effect = DragDropEffects.Move;
            };


            ObjectTreeView.GiveFeedback += (sender, e) =>
            {
                // Use custom cursors if the check box is checked.
                // Sets the custom cursor based upon the effect.
                //InputManager.
                if(InputLibrary.Cursor.Self.IsInWindow)
                {
                    e.UseDefaultCursors = false;
                    System.Windows.Forms.Cursor.Current = AddCursor;
                }

                //if ((e.Effect & DragDropEffects.Move) == DragDropEffects.Move)
                //    Cursor.Current = MyNormalCursor;
                //else
                //    Cursor.Current = MyNoDropCursor;
            };
        }

        private void ObjectTreeView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            int m = 3;
        }

        private void ObjectTreeView_KeyPress(object sender, KeyPressEventArgs e)
        {
            DragDropManager.Self.HandleKeyPress(e);
        }

        private void HandleDragDropEvent(object sender, DragEventArgs e)
        {
            if(e.Data != null)
            {
                DragDropManager.Self.HandleDragDropEvent(sender, e);
            }
            DragDropManager.Self.ClearDraggedItem();
        }

        private void AddAndRemoveFolderNodes()
        {
            if (ObjectFinder.Self.GumProjectSave != null && 
                
                !string.IsNullOrEmpty(ObjectFinder.Self.GumProjectSave.FullFileName))
            {
                string currentDirectory = FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName);

                // Let's make sure these folders exist, they better!
                Directory.CreateDirectory(mStandardElementsTreeNode.GetFullFilePath().FullPath);
                Directory.CreateDirectory(mScreensTreeNode.GetFullFilePath().FullPath);
                Directory.CreateDirectory(mComponentsTreeNode.GetFullFilePath().FullPath);
                Directory.CreateDirectory(mBehaviorsTreeNode.GetFullFilePath().FullPath);


                // add folders to the screens, entities, and standard elements
                AddAndRemoveFolderNodes(mStandardElementsTreeNode.GetFullFilePath().FullPath, mStandardElementsTreeNode.Nodes);
                AddAndRemoveFolderNodes(mScreensTreeNode.GetFullFilePath().FullPath, mScreensTreeNode.Nodes);
                AddAndRemoveFolderNodes(mComponentsTreeNode.GetFullFilePath().FullPath, mComponentsTreeNode.Nodes);
                AddAndRemoveFolderNodes(mBehaviorsTreeNode.GetFullFilePath().FullPath, mBehaviorsTreeNode.Nodes);

                //AddAndRemoveFolderNodes(currentDirectory, this.mTreeView.Nodes);
            }
            else
            {
                RootComponentsTreeNode.Nodes.Clear();
            }
        }

        private void AddAndRemoveFolderNodes(string currentDirectory, TreeNodeCollection nodesToAddTo)
        {
            // todo: removes
            var directories = Directory.EnumerateDirectories(currentDirectory).ToArray();

            foreach (string directory in directories)
            {
                TreeNode existingTreeNode = GetTreeNodeFor(directory);

                if (existingTreeNode == null)
                {
                    existingTreeNode = nodesToAddTo.Add(FileManager.RemovePath(directory));
                    existingTreeNode.ImageIndex = FolderImageIndex;
                }
                AddAndRemoveFolderNodes(directory, existingTreeNode.Nodes);
            }

            for(int i = nodesToAddTo.Count - 1; i > -1; i--)
            {
                TreeNode node = nodesToAddTo[i];

                bool found = false;

                foreach (string directory in directories)
                {
                    string directoryStripped = FileManager.RemovePath(directory);

                    if (directoryStripped.Equals(node.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                // only remove nodes if they are directory nodes (aka they have a null tag)
                if (!found && node.Tag == null)
                {
                    nodesToAddTo.RemoveAt(i);
                }               
            }
        }

        bool ShouldShow(ScreenSave screen) => string.IsNullOrEmpty(filterText) || screen.Name.ToLower().Contains(filterText.ToLower());
        bool ShouldShow(ComponentSave component) => string.IsNullOrEmpty(filterText) || component.Name.ToLower().Contains(filterText.ToLower());
        bool ShouldShow(StandardElementSave standardElementSave) => string.IsNullOrEmpty(filterText) || standardElementSave.Name.ToLower().Contains(filterText.ToLower());
        bool ShouldShow(BehaviorSave behavior) => string.IsNullOrEmpty(filterText) || behavior.Name?.ToLower().Contains(filterText.ToLower()) == true;

        private void AddAndRemoveScreensComponentsStandardsAndBehaviors(TreeNode folderTreeNode)
        {
            /////////////Early Out////////////////
            if (ProjectManager.Self.GumProjectSave == null)
                return;
            ////////////End Early Out////////////

            // Save off old selected stuff
            InstanceSave selectedInstance = SelectedState.Self.SelectedInstance;
            ElementSave selectedElement = SelectedState.Self.SelectedElement;
            BehaviorSave selectedBehavior = SelectedState.Self.SelectedBehavior;


            #region Add nodes that haven't been added yet

            foreach (ScreenSave screenSave in ProjectManager.Self.GumProjectSave.Screens)
            {
                var treeNode = GetTreeNodeFor(screenSave);
                if (treeNode == null && ShouldShow(screenSave))
                {
                    string fullPath = FileLocations.Self.ScreensFolder + FileManager.GetDirectory(screenSave.Name);
                    TreeNode parentNode = GetTreeNodeFor(fullPath);

                    treeNode = AddTreeNodeForElement(screenSave, parentNode, ScreenImageIndex);
                }
            }

            foreach (ComponentSave componentSave in ProjectManager.Self.GumProjectSave.Components)
            {
                if (GetTreeNodeFor(componentSave) == null && ShouldShow(componentSave))
                {
                    string fullPath = FileLocations.Self.ComponentsFolder + FileManager.GetDirectory(componentSave.Name);
                    TreeNode parentNode = GetTreeNodeFor(fullPath);

                    if(parentNode == null)
                    {
                        throw new Exception($"Error trying to get parent node for component {fullPath}");
                    }

                    AddTreeNodeForElement(componentSave, parentNode, ComponentImageIndex);
                }
            }

            foreach (StandardElementSave standardSave in ProjectManager.Self.GumProjectSave.StandardElements)
            {
                if (standardSave.Name != "Component")
                {
                    if (GetTreeNodeFor(standardSave) == null &&  ShouldShow(standardSave))
                    {
                        AddTreeNodeForElement(standardSave, mStandardElementsTreeNode, StandardElementImageIndex);
                    }
                }
            }

            foreach(BehaviorSave behaviorSave in ProjectManager.Self.GumProjectSave.Behaviors)
            {
                if(GetTreeNodeFor(behaviorSave) == null && ShouldShow(behaviorSave))
                {
                    string fullPath = FileLocations.Self.BehaviorsFolder;
                    
                    if(behaviorSave.Name != null)
                    {
                        fullPath = FileLocations.Self.BehaviorsFolder + FileManager.GetDirectory(behaviorSave.Name);
                    }
                    TreeNode parentNode = GetTreeNodeFor(fullPath);

                    AddTreeNodeForBehavior(behaviorSave, parentNode, BehaviorImageIndex);
                }
            }

            #endregion

            #region Remove nodes that are no longer needed

            void RemoveScreenRecursively(TreeNode treeNode, int i, TreeNode container)
            {
                ScreenSave screen = treeNode.Tag as ScreenSave;

                // If the screen is null, that means that it's a folder TreeNode, so we don't want to remove it
                if (screen != null)
                {
                    if (!ProjectManager.Self.GumProjectSave.Screens.Contains(screen) || !ShouldShow(screen))
                    {
                        container.Nodes.RemoveAt(i);
                    }
                }
                else if(treeNode.Nodes != null)
                {
                    for(int subI = treeNode.Nodes.Count - 1; subI > -1; subI--)
                    {
                        var subnode = treeNode.Nodes[subI];
                        RemoveScreenRecursively(subnode, subI, treeNode);
                    }
                }
            }

            for (int i = mScreensTreeNode.Nodes.Count - 1; i > -1; i--)
            {
                RemoveScreenRecursively(mScreensTreeNode.Nodes[i] as TreeNode, i, mScreensTreeNode);
            }

            void RemoveComponentRecursively(TreeNode treeNode, int i, TreeNode container)
            {
                ComponentSave component = treeNode.Tag as ComponentSave;

                // If the component is null, that means that it's a folder TreeNode, so we don't want to remove it
                if (component != null)
                {
                    if (!ProjectManager.Self.GumProjectSave.Components.Contains(component) || !ShouldShow(component))
                    {
                        container.Nodes.RemoveAt(i);
                    }
                }
                else if (treeNode.Nodes != null)
                {
                    for (int subI = treeNode.Nodes.Count - 1; subI > -1; subI--)
                    {
                        var subnode = treeNode.Nodes[subI];
                        RemoveComponentRecursively(subnode, subI, treeNode);
                    }
                }
            }

            for (int i = mComponentsTreeNode.Nodes.Count - 1; i > -1; i--)
            {
                RemoveComponentRecursively(mComponentsTreeNode.Nodes[i], i, mComponentsTreeNode);
            }

            for (int i = mStandardElementsTreeNode.Nodes.Count - 1; i > -1; i-- )
            {
                // Do we want to support folders here?
                StandardElementSave standardElement = mStandardElementsTreeNode.Nodes[i].Tag as StandardElementSave;

                if (!ProjectManager.Self.GumProjectSave.StandardElements.Contains(standardElement) || !ShouldShow(standardElement))
                {
                    mStandardElementsTreeNode.Nodes.RemoveAt(i);
                }
            }

            for(int i = mBehaviorsTreeNode.Nodes.Count - 1; i > -1; i--)
            {
                BehaviorSave behavior = mBehaviorsTreeNode.Nodes[i].Tag as BehaviorSave;

                if(behavior != null)
                {
                    if(!ProjectManager.Self.GumProjectSave.Behaviors.Contains(behavior) || !ShouldShow(behavior))
                    {
                        mBehaviorsTreeNode.Nodes.RemoveAt(i);
                    }
                }
            }

            #endregion

            #region Update the nodes

            System.Collections.IList list = mScreensTreeNode.Nodes;
            for (int i = 0; i < list.Count; i++)
            {
                object treeNode = list[i];
                RefreshUi(treeNode as TreeNode);
            }

            foreach (TreeNode treeNode in mComponentsTreeNode.Nodes)
            {
                RefreshUi(treeNode);
            }

            foreach (TreeNode treeNode in mStandardElementsTreeNode.Nodes)
            {
                RefreshUi(treeNode);
            }

            foreach(TreeNode treeNode in mBehaviorsTreeNode.Nodes)
            {
                RefreshUi(treeNode);
            }

            #endregion

            #region Sort everything

            mScreensTreeNode.Nodes.SortByName(recursive:true);

            mComponentsTreeNode.Nodes.SortByName(recursive: true);

            mStandardElementsTreeNode.Nodes.SortByName(recursive: true);

            mBehaviorsTreeNode.Nodes.SortByName(recursive: true);

            #endregion

            #region Re-select whatever was selected before

            if (selectedInstance != null)
            {
                SelectedState.Self.SelectedInstance = selectedInstance;
            }
            if(selectedBehavior != null)
            {
                SelectedState.Self.SelectedBehavior = selectedBehavior;
            }
            #endregion
        }

        private static TreeNode AddTreeNodeForElement(ElementSave element, TreeNode parentNode, int defaultImageIndex)
        {
            if (parentNode == null)
            {
                throw new NullReferenceException($"{nameof(parentNode)} cannot be null");
            }
            TreeNode treeNode = new TreeNode();

            if (element.IsSourceFileMissing)
                treeNode.ImageIndex = ExclamationIndex;
            else
                treeNode.ImageIndex = defaultImageIndex;

            treeNode.Tag = element;
            
            parentNode.Nodes.Add(treeNode);

            return treeNode;
        }

        private static void AddTreeNodeForBehavior(BehaviorSave behavior, TreeNode parentNode, int defaultImageIndex)
        {
            TreeNode treeNode = new TreeNode();

            if (behavior.IsSourceFileMissing)
                treeNode.ImageIndex = ExclamationIndex;
            else
                treeNode.ImageIndex = defaultImageIndex;

            treeNode.Tag = behavior;

            parentNode.Nodes.Add(treeNode);
        }

        private void CreateRootTreeNodesIfNecessary()
        {
            if (mScreensTreeNode == null)
            {
                mScreensTreeNode = new TreeNode("Screens");
                mScreensTreeNode.ImageIndex = FolderImageIndex;
                ObjectTreeView.Nodes.Add(mScreensTreeNode);

                mComponentsTreeNode = new TreeNode("Components");
                mComponentsTreeNode.ImageIndex = FolderImageIndex;
                ObjectTreeView.Nodes.Add(mComponentsTreeNode);

                mStandardElementsTreeNode = new TreeNode("Standard");
                mStandardElementsTreeNode.ImageIndex = FolderImageIndex;
                ObjectTreeView.Nodes.Add(mStandardElementsTreeNode);

                mBehaviorsTreeNode = new TreeNode("Behaviors");
                mBehaviorsTreeNode.ImageIndex = FolderImageIndex;
                ObjectTreeView.Nodes.Add(mBehaviorsTreeNode);
            }
        }


        public void RecordSelection()
        {
            mRecordedSelectedObject = SelectedState.Self.SelectedInstance;

            if (mRecordedSelectedObject == null)
            {
                mRecordedSelectedObject = SelectedState.Self.SelectedElement;
            }

            if(mRecordedSelectedObject == null)
            {
                mRecordedSelectedObject = SelectedState.Self.SelectedBehavior;
            }
        }

        public void SelectRecordedSelection()
        {
            if (mRecordedSelectedObject != null)
            {
                if (mRecordedSelectedObject is InstanceSave)
                {
                    SelectedState.Self.SelectedInstance = mRecordedSelectedObject as InstanceSave;
                }
                else if (mRecordedSelectedObject is ElementSave)
                {
                    SelectedState.Self.SelectedElement = mRecordedSelectedObject as ElementSave;
                }
                else if(mRecordedSelectedObject is BehaviorSave)
                {
                    SelectedState.Self.SelectedBehavior = mRecordedSelectedObject as BehaviorSave;
                }
            }
        }

        // Discussion about Selection
        // Selection is a rather complicated
        // system in Gum because tree nodes can
        // be selected in a number of ways:
        // 1.  The user can push/release (click)
        // 2.  The user can select an item in the
        //     wireframe window which in turn selects
        //     the appropriate tree node.
        // 3.  The user pushes on a tree node, but then
        //     drags off of it to do a drag+drop somewhere
        //     else.
        // We want the app to refresh what it is displaying
        // in scenario 1 and 2, but not in 3.  Therefore the
        // MultiSelectTreeView class has an event called AfterClickSelect
        // which only fires when the user actually clicks on an item (1) so
        // that #3 doesn't fire off an event.  However, this means that #2 will
        // no longer fire off the event either.  We need to then make sure that #2
        // does still fire off an event, so we'll do this by manually raising the event
        // in the Select methods where a Save object is selected.
        public void Select(InstanceSave instanceSave, ElementSave parent)
        {
            if (IsInUiInitiatedSelection) return;
            if (instanceSave != null)
            {
                TreeNode parentTreeNode = GetTreeNodeFor(parent);

                // This could be null if the user started a new project or loaded a different project.
                if (parentTreeNode != null)
                {
                    Select(GetTreeNodeFor(instanceSave, parentTreeNode));
                }
            }
            else
            {
                Select((TreeNode)null);
            }
        }

        public void Select(BehaviorSave behavior)
        {
            if (IsInUiInitiatedSelection) return;

            if (behavior != null)
            {
                var treeNode = GetTreeNodeFor(behavior);

                Select(treeNode);
            }
        }

        public void Select(IEnumerable<InstanceSave> list)
        {
            if (IsInUiInitiatedSelection) return;

            if (list.Count() != 0)
            {
                var firstItem = list.First();

                TreeNode parentContainer = null;
                if(firstItem.ParentContainer != null)
                {
                    parentContainer = GetTreeNodeFor(firstItem.ParentContainer);
                }
                else
                {
                    var behavior = ObjectFinder.Self.GetBehaviorContainerOf(firstItem);
                    if(behavior != null)
                    {
                        parentContainer = GetTreeNodeFor(behavior);
                    }
                }

                List<TreeNode> treeNodeList = new List<TreeNode>();

                foreach (var item in list)
                {
                    if(parentContainer != null)
                    {
                        TreeNode itemTreeNode = GetTreeNodeFor(item, parentContainer);
                        treeNodeList.Add(itemTreeNode);
                    }
                }

                Select(treeNodeList);
            }
            else
            {
                Select((TreeNode)null);
            }
        }


        public void Select(ElementSave elementSave)
        {
            if (IsInUiInitiatedSelection) return;

            if (elementSave == null)
            {
                if (ObjectTreeView.SelectedNode != null && ObjectTreeView.SelectedNode.Tag != null && ObjectTreeView.SelectedNode.Tag is ElementSave)
                {
                    // why do we explicitly set this here rather than calling Select? If we set it to null without calling that, we don't get the benefit of the 
                    // plugins being notified of a null selection:
                    //ObjectTreeView.SelectedNode = null;
                    Select((TreeNode)null);

                }
            }
            else
            {
                var treeNode = GetTreeNodeFor(elementSave);

                if(treeNode == null && !string.IsNullOrEmpty(searchTextBox.Text))
                {
                    searchTextBox.Text = null;
                    treeNode = GetTreeNodeFor(elementSave);
                }

                Select(treeNode);
            }
        }

        private void Select(TreeNode treeNode)
        {
            if (IsInUiInitiatedSelection) return;

            if (ObjectTreeView.SelectedNode != treeNode)
            {
                // See comment above about why we have to manually raise the AfterClick

                ObjectTreeView.SelectedNode = treeNode;

                if (treeNode != null)
                {
                    treeNode.EnsureVisible();
                }

                ObjectTreeView.CallAfterClickSelect(null, new TreeViewEventArgs(treeNode));
            }
        }

        private void Select(List<TreeNode> treeNodes)
        {
            if (IsInUiInitiatedSelection) return;

            ObjectTreeView.SelectedNodes = treeNodes;

            if (treeNodes.Count != 0)
            {
                treeNodes[0]?.EnsureVisible();
                ObjectTreeView.CallAfterClickSelect(null, new TreeViewEventArgs(treeNodes[0]));
            }
        }

        /// <summary>
        /// Refreshes the entirety of the tree view, preserving selection.
        /// </summary>
        public void RefreshUi()
        {
            RecordSelection();
            // brackets are used simply to indicate the recording and selection should
            // go around the rest of the function:
            {
                ObjectTreeView.SuspendLayout();
                CreateRootTreeNodesIfNecessary();

                AddAndRemoveFolderNodes();

                AddAndRemoveScreensComponentsStandardsAndBehaviors(null);
                ObjectTreeView.ResumeLayout(performLayout:true);

            }
            SelectRecordedSelection();
        }


        public void RefreshUi(IInstanceContainer instanceContainer)
        {
            var foundNode = GetTreeNodeForTag(instanceContainer);

            if(foundNode != null)
            {
                RecordSelection();
                RefreshUi(foundNode);
                SelectRecordedSelection();
            }
        }

        /// <summary>
        /// Refreshes the tree nodes for the argument stateContainer. This includes the displayed text and contained nodes, and the parent
        /// folder node.
        /// </summary>
        /// <param name="stateContainer">The StateContainer to refresh.</param>
        public void RefreshUi(IStateContainer stateContainer)
        {
            var foundNode = GetTreeNodeForTag(stateContainer);

            if(foundNode != null)
            {
                RecordSelection();
                RefreshUi(foundNode);
                SelectRecordedSelection();
            }
        }

        public void RefreshUi(TreeNode node)
        {
            if(node  == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.Tag is ElementSave)
            {
                ElementSave elementSave = node.Tag as ElementSave;

                RefreshElementTreeNode(node, elementSave);
            }
            else if (node.Tag is InstanceSave)
            {
                InstanceSave instanceSave = node.Tag as InstanceSave;
                // this if check improves speed quite a bit!
                if(instanceSave.Name != node.Text)
                {
                    node.Text = instanceSave.Name;
                }
            }
            else if(node.Tag is BehaviorSave behaviorSave)
            {
                var behavior = node.Tag as BehaviorSave;
                if(behavior.Name != node.Text)
                {
                    node.Text = behavior.Name;
                }
                RefreshBehaviorTreeNode(node, behaviorSave);
            }

            foreach (TreeNode treeNode in node.Nodes)
            {
                if(treeNode != null)
                {
                    RefreshUi(treeNode);
                }

            }
        }

        private void RefreshElementTreeNode(TreeNode node, ElementSave elementSave)
        {
            List<InstanceSave> expandedInstances = new List<InstanceSave>();
            List<InstanceSave> allInstances = elementSave.Instances;

            if(elementSave is ScreenSave || elementSave is ComponentSave)
            {

                string fullPath = null;
                if(elementSave is ScreenSave)
                {
                    fullPath = FileLocations.Self.ScreensFolder + FileManager.GetDirectory(elementSave.Name);
                }
                else
                {
                    fullPath = FileLocations.Self.ComponentsFolder + FileManager.GetDirectory(elementSave.Name);
                }
                TreeNode desiredNode = GetTreeNodeFor(fullPath);
                var parentNode = node.Parent;
                if(parentNode != desiredNode)
                {
                    if (parentNode != null)
                    {
                        parentNode.Nodes.Remove(node);
                    }
                    if(desiredNode != null)
                    {
                        desiredNode.Nodes.Add(node);
                    }
                }
            }

            foreach (InstanceSave instance in allInstances)
            {
                // use name because an undo can change references. Same with reloads if were called there
                var treeNode = GetInstanceTreeNodeByName(instance.Name, node);

                if (treeNode?.Nodes.Count > 0 && treeNode?.IsExpanded == true)
                {
                    expandedInstances.Add(instance);
                }
            }

            var nodeText = FileManager.RemovePath(elementSave.Name);
            if(nodeText != node.Text)
            {
                var hadTextBefore = !string.IsNullOrEmpty(node.Text);
                node.Text = nodeText;

                if(hadTextBefore && node.Parent != null)
                {
                    node.Parent.Nodes.SortByName();
                }
            }

            var allTreeNodesRecursively = node.GetAllChildrenNodesRecursively();
            
            // why do we clear? wouldn't this require re-creation of all nodes? that seems like it might be slow...
            //node.Nodes.Clear();
            // Let's be smart about removal...
            foreach(TreeNode instanceNode in allTreeNodesRecursively)
            {
                var instance = instanceNode.Tag as InstanceSave;

                if(!allInstances.Contains(instance))
                {
                    instanceNode.Remove();
                }
            }


            foreach (InstanceSave instance in allInstances)
            {
                TreeNode nodeForInstance = GetTreeNodeFor(instance, node);

                if (nodeForInstance == null)
                {
                    nodeForInstance = AddTreeNodeForInstance(instance, node, tolerateMissingTypes:false);
                }

                if(instance.DefinedByBase)
                {
                    nodeForInstance.ImageIndex = DerivedInstanceImageIndex;
                }

                // todo - do this after we have all the children created:
                if (expandedInstances.Any(item => item.Name == instance.Name))
                {
                    nodeForInstance.Expand();
                }

                var siblingInstances = instance.GetSiblingsIncludingThis();
                var desiredIndex = siblingInstances.IndexOf(instance);

                var container = instance.ParentContainer;
                var defaultState = container.DefaultState;
                //var thisParentValue = defaultState.GetValueOrDefault<string>($"{instance.Name}.Parent");
                var thisParentValue = defaultState.GetValueRecursive($"{instance.Name}.Parent") as string;

                // If thisParentValue has a period, the instance is attached to an item inside the parent.
                if(thisParentValue?.Contains(".") == true)
                {
                    thisParentValue = thisParentValue.Substring(0, thisParentValue.IndexOf('.'));
                }

                var desiredParentNode = node;
                if(!string.IsNullOrEmpty(thisParentValue))
                {
                    var instanceParent = allInstances.FirstOrDefault(item => item.Name == thisParentValue);

                    if(instanceParent != null)
                    {
                        desiredParentNode = GetTreeNodeFor(instanceParent, node);
                    }
                }
                if(desiredParentNode != nodeForInstance.Parent && desiredParentNode != null)
                {
                    nodeForInstance.Remove();
                    desiredParentNode.Nodes.Add(nodeForInstance);
                }

                var nodeParent = nodeForInstance.Parent;
                if (desiredIndex != nodeParent.Nodes.IndexOf(nodeForInstance))
                {
                    nodeParent.Nodes.Remove(nodeForInstance);
                    nodeParent.Nodes.Insert(desiredIndex, nodeForInstance);
                }

                var element = ObjectFinder.Self.GetElementSave(instance.BaseType);

                int desiredImageIndex = InstanceImageIndex;
                if (element == null || element.IsSourceFileMissing)
                    desiredImageIndex = ExclamationIndex;

                if(nodeForInstance.ImageIndex != desiredImageIndex)
                {
                    nodeForInstance.ImageIndex = desiredImageIndex;
                }
            }

            foreach(var expandedInstance in expandedInstances)
            {
                var toExpand = GetInstanceTreeNodeByName(expandedInstance.Name, node);
                toExpand?.Expand();
            }
        }

        private void RefreshBehaviorTreeNode(TreeNode node, BehaviorSave behavior)
        {
            var allInstances = behavior.RequiredInstances;
            var allTreeNodesRecursively = node.GetAllChildrenNodesRecursively();
            foreach (TreeNode instanceNode in allTreeNodesRecursively)
            {
                var instance = instanceNode.Tag as InstanceSave;

                if (!allInstances.Contains(instance))
                {
                    instanceNode.Remove();
                }
            }


            foreach (InstanceSave instance in allInstances)
            {
                TreeNode nodeForInstance = GetTreeNodeFor(instance, node);

                if (nodeForInstance == null)
                {
                    nodeForInstance = AddTreeNodeForInstance(instance, node, tolerateMissingTypes:true);
                }
                if (instance.DefinedByBase)
                {
                    nodeForInstance.ImageIndex = DerivedInstanceImageIndex;
                }
                // screens have to worry about siblings and lists. We don't care about that here because behaviors do not
                // (currently) require instances to have a particular relationship with one another
            }
        }

        private TreeNode AddTreeNodeForInstance(InstanceSave instance, TreeNode parentContainerNode, bool tolerateMissingTypes, HashSet<InstanceSave> pendingAdditions = null)
        {
            TreeNode treeNode = new TreeNode();

            bool validBaseType = ObjectFinder.Self.GetElementSave(instance.BaseType) != null;

            if (validBaseType || tolerateMissingTypes)
                treeNode.ImageIndex = InstanceImageIndex;
            else
                treeNode.ImageIndex = ExclamationIndex;

            treeNode.Tag = instance;

            TreeNode parentNode = parentContainerNode;
            InstanceSave parentInstance = FindParentInstance(instance);

            if (parentInstance != null)
            {
                TreeNode parentInstanceNode = GetTreeNodeFor(parentInstance, parentContainerNode);

                // Make sure we are not already trying to add the parent (protects against stack overflow with invalid data)
                if (parentInstanceNode == null && (pendingAdditions == null || !pendingAdditions.Contains(parentInstance)))
                {
                    if (pendingAdditions == null)
                    {
                        pendingAdditions = new HashSet<InstanceSave>();
                    }

                    pendingAdditions.Add(parentInstance);
                    parentInstanceNode = AddTreeNodeForInstance(parentInstance, parentContainerNode, tolerateMissingTypes, pendingAdditions);
                }

                if (parentInstanceNode != null)
                {
                    parentNode = parentInstanceNode;
                }
            }

            parentNode.Nodes.Add(treeNode);

            return treeNode;
        }

        private InstanceSave FindParentInstance(InstanceSave instance)
        {
            if(instance is BehaviorInstanceSave)
            {
                // instances in behaviors cannot (currently) have parents
                return null;
            }
            else
            {
                ElementSave element = instance.ParentContainer;

                string name = instance.Name + ".Parent";
                VariableSave variable = element.DefaultState.Variables.FirstOrDefault(v => v.Name == name);

                if (variable != null && variable.SetsValue && variable.Value != null)
                {
                    string parentName = (string) variable.Value;

                    // This could be attached to a child inside the parent. Therefore, if ParentInstance contains a dot, return 
                    // the instance with the name before the dot
                    if (parentName.Contains('.'))
                    {
                        parentName = parentName.Substring(0, parentName.IndexOf('.'));
                    }

                    return element.GetInstance(parentName);
                }
            }

            return null;
        }

        bool IsInUiInitiatedSelection = false;
        internal void OnSelect(TreeNode selectedTreeNode)
        {
            TreeNode treeNode = ObjectTreeView.SelectedNode;

            object selectedObject = null;

            if (treeNode != null)
            {
                selectedObject = treeNode.Tag;
            }


            try
            {
                IsInUiInitiatedSelection = true;
                if (selectedObject == null)
                {
                    SelectedState.Self.SelectedElement = null;
                    SelectedState.Self.SelectedBehavior = null;
                    SelectedState.Self.SelectedInstance = null;

                    // do nothing
                }
                else if(selectedObject is ElementSave elementSave)
                {
                    SelectedState.Self.SelectedInstance = null;
                    SelectedState.Self.SelectedElement = elementSave;
                }
                else if (selectedObject is InstanceSave selectedInstance)
                {
                    var instances = this.SelectedNodes.Select(item => item.Tag)
                        .Where(item => item is InstanceSave)
                        .Select(item => item as InstanceSave);

                    //SelectedState.Self.SelectedInstance = selectedInstance;
                    SelectedState.Self.SelectedInstances = instances;
                }
                else if(selectedObject is BehaviorSave behavior)
                {
                    SelectedState.Self.SelectedBehavior = behavior;
                }

                PluginManager.Self.TreeNodeSelected(selectedTreeNode);

            }
            finally
            {
                IsInUiInitiatedSelection = false;
            }
        }

        public void VerifyComponentsAreInTreeView(GumProjectSave gumProject)
        {
            foreach (ComponentSave component in gumProject.Components)
            {
                if (GetTreeNodeFor(component) == null)
                {
                    throw new Exception();
                }
            }
        }

        internal void HandleKeyDown(KeyEventArgs e)
        {
            HotkeyManager.Self.HandleKeyDownElementTreeView(e);
        }

        private void ObjectTreeView_AfterSelect_1(object sender, TreeViewEventArgs e)
        {
            // If we use AfterClickSelect instead of AfterSelect then
            // we don't get notified when the user selects nothing.
            // Update - we only want to do this if it's null:
            // Otherwise we can't drag drop
            if (ObjectTreeView.SelectedNode == null)
            {
                ElementTreeViewManager.Self.OnSelect(ObjectTreeView.SelectedNode);
            }
        }

        private void ObjectTreeView_AfterClickSelect(object sender, TreeViewEventArgs e)
        {
            ElementTreeViewManager.Self.OnSelect(ObjectTreeView.SelectedNode);
        }

        private void ObjectTreeView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ElementTreeViewManager.Self.OnSelect(ObjectTreeView.SelectedNode);

                ElementTreeViewManager.Self.PopulateMenuStrip();
            }
        }

        private void ObjectTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            ElementTreeViewManager.Self.HandleKeyDown(e);
            DragDropManager.Self.HandleKeyDown(e);
            HotkeyManager.Self.HandleKeyDownAppWide(e);
        }


        #endregion

        #region Searching

        private FlatSearchListBox CreateFlatSearchList()
        {
            var list = new FlatSearchListBox();
            list.SelectSearchNode += HandleSelectedSearchNode;
            return list;
        }


        private void ReactToFilterTextChanged()
        {
            var shouldExpand = false;

            if (!string.IsNullOrEmpty(filterText))
            {
                shouldExpand = true;
            }

            FlatList.Visibility = shouldExpand.ToVisibility();
            TreeViewHost.Visibility = (!shouldExpand).ToVisibility();

            //RefreshUi();

            if (!string.IsNullOrEmpty(filterText) && SelectedNode?.Tag == null)
            {
                //SelectFirstElement();
            }


            if (shouldExpand)
            {
                var filterTextLower = filterText?.ToLower();
                FlatList.FlatList.Items.Clear();

                var project = GumState.Self.ProjectState.GumProjectSave;
                foreach (var screen in project.Screens)
                {
                    if (screen.Name.ToLower().Contains(filterTextLower))
                    {
                        AddToFlatList(screen);
                    }

                    if (deepSearchCheckBox.Checked)
                    {
                        SearchInstanceVariables(screen, filterTextLower);
                    }
                }
                foreach (var component in project.Components)
                {
                    if (component.Name.ToLower().Contains(filterTextLower))
                    {
                        AddToFlatList(component);
                    }

                    foreach (var instance in component.Instances)
                    {
                        if (instance.Name.ToLower().Contains(filterTextLower))
                        {
                            AddToFlatList(instance, $"{component.Name}/{instance.Name} ({instance.BaseType})");
                        }
                    }

                    if (deepSearchCheckBox.Checked)
                    {
                        SearchInstanceVariables(component, filterTextLower);
                    }
                }
                foreach (var standard in project.StandardElements)
                {
                    if (standard.Name.ToLower().Contains(filterTextLower))
                    {
                        AddToFlatList(standard);
                    }

                    if (deepSearchCheckBox.Checked)
                    {
                        SearchInstanceVariables(standard, filterTextLower);
                    }
                }

                foreach(var behavior in project.Behaviors)
                {
                    if(behavior.Name.ToLower().Contains(filterTextLower))
                    {
                        AddToFlatList(behavior);
                    }
                }

                if(FlatList.FlatList.Items.Count > 0)
                {
                    FlatList.FlatList.SelectedIndex = 0;
                }
            }

            //do this after refreshing the UI or else the tree nodes won't expand
            //if (string.IsNullOrEmpty(filterText))
            //{
            //    List<TreeNode> nodesToKeepExpanded = new List<TreeNode>();

            //    var node = SelectedNode;

            //    while (node != null)
            //    {
            //        nodesToKeepExpanded.Add(node);
            //        node = node.Parent;
            //    }

            //    if (expandedStateBeforeFilter != null)
            //    {
            //        expandedStateBeforeFilter.Apply();
            //        expandedStateBeforeFilter = null;
            //    }

            //    SelectedNode?.EnsureVisible();
            //}
        }

        private void SearchInstanceVariables(ElementSave element, string filterTextLower )
        {
            foreach (var state in element.AllStates)
            {
                foreach (var variable in state.Variables)
                {
                    if (variable == null)
                    {
                        continue;
                    }

                    if (variable.Value != null && (variable.Value is string str) && str.ToLower().Contains(filterTextLower))
                    {
                        var instance = element.Instances.FirstOrDefault(item => item.Name == variable.SourceObject);
                        if(instance != null)
                        {
                            AddToFlatList(instance, $"{variable.Name}={variable.Value} on {element.Name}/{variable.SourceObject}");
                        }
                        else
                        {
                            AddToFlatList(element, $"{variable.Name}={variable.Value} on {element.Name}");
                        }
                    }
                }
            }
        }

        private void AddToFlatList(object element, string customName = "")
        {
            if (element == null)
            {
                throw new ArgumentNullException($"{nameof(element)}");
            }
            var vm = new SearchItemViewModel();
            vm.BackingObject = element;
            vm.CustomText = customName;
            FlatList.FlatList.Items.Add(vm);
        }

        private Control CreateSearchBoxUi()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Top;

            searchTextBox = new TextBox();
            searchTextBox.TextChanged += (not, used) => FilterText = searchTextBox.Text;
            searchTextBox.KeyDown += (sender, args) =>
            {
                if (args.KeyCode == Keys.Escape)
                {
                    searchTextBox.Text = null;
                    args.Handled = true;
                    args.SuppressKeyPress = true;
                    ObjectTreeView.Focus();
                }
                else if (args.KeyCode == Keys.Back
                 && (args.Modifiers & Keys.Control) == Keys.Control
                )
                {
                    searchTextBox.Text = null;
                    args.Handled = true;
                    args.SuppressKeyPress = true;
                }
                else if (args.KeyCode == Keys.Down)
                {
                    if(FlatList.FlatList.SelectedIndex < FlatList.FlatList.Items.Count -1)
                    {
                        FlatList.FlatList.SelectedIndex++;
                    }
                    args.Handled = true;
                }
                else if (args.KeyCode == Keys.Up)
                {
                    if (FlatList.FlatList.SelectedIndex > 0)
                    {
                        FlatList.FlatList.SelectedIndex--;
                    }
                    args.Handled = true;
                }
                else if (args.KeyCode == Keys.Enter)
                {
                    args.Handled = true;
                    args.SuppressKeyPress = true;
                    ObjectTreeView.Focus();

                    var selectedItem = FlatList.FlatList.SelectedItem as SearchItemViewModel;
                    if(selectedItem != null)
                    {
                        HandleSelectedSearchNode(selectedItem);

                        searchTextBox.Text = null;
                    }
                }
            };
            searchTextBox.Dock = DockStyle.Fill;
            panel.Controls.Add(searchTextBox);

            var xButton = new Button();
            xButton.Text = "X";
            xButton.Click += (not, used) => searchTextBox.Text = null;
            xButton.Dock = DockStyle.Right;
            xButton.Width = 24;
            panel.Controls.Add(xButton);
            panel.Height = 20;

            return panel;
        }

        private Control CreateSearchCheckBoxUi()
        {
            var panel = new Panel();

            deepSearchCheckBox = new CheckBox();
            deepSearchCheckBox.Checked = false;
            deepSearchCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            deepSearchCheckBox.Width = 200;
            deepSearchCheckBox.Text = "Search variables";
            deepSearchCheckBox.CheckedChanged += (object sender, EventArgs args) =>
            {
                ReactToFilterTextChanged();
            };

            panel.Controls.Add(deepSearchCheckBox);

            return panel;
        }

        private void HandleSelectedSearchNode(SearchItemViewModel vm)
        {
            var backingObject = vm?.BackingObject;
            if(backingObject != null)
            {
                if (backingObject is ScreenSave asScreen)
                    GumState.Self.SelectedState.SelectedElement = asScreen;
                else if (backingObject is ComponentSave asComponent)
                    GumState.Self.SelectedState.SelectedElement = asComponent;
                else if (backingObject is StandardElementSave asStandard)
                    GumState.Self.SelectedState.SelectedElement = asStandard;
                else if (backingObject is InstanceSave asInstance)
                    GumState.Self.SelectedState.SelectedInstance = asInstance;
                else if (backingObject is VariableSave asVariable)
                    GumState.Self.SelectedState.SelectedBehaviorVariable = asVariable;
                else if(backingObject is BehaviorSave asBehavior)
                    GumState.Self.SelectedState.SelectedBehavior = asBehavior;

                searchTextBox.Text = null;
                FilterText = null;
            }
        }


        #endregion


        internal void HandleMouseOver(int x, int y)
        {
            var objectOver = this.ObjectTreeView.GetNodeAt(x, y);

            ElementSave element = null;
            InstanceSave instance = null;

            if(objectOver != null && objectOver.Tag != null)
            {
                if(objectOver.Tag is ElementSave)
                {
                    element = objectOver.Tag as ElementSave;
                }
                else if(objectOver.Tag is InstanceSave)
                {
                    instance = objectOver.Tag as InstanceSave;
                }
            }

            GraphicalUiElement whatToHighlight = null;

            if(element != null)
            {
                whatToHighlight = WireframeObjectManager.Self.GetRepresentation(element);
            }
            else if(instance != null)
            {
                whatToHighlight = WireframeObjectManager.Self.GetRepresentation(instance, null);
            }

            SelectionManager.Self.HighlightedIpso = whatToHighlight;
        }
    }


    #region TreeNodeExtensionMethods

    public static class TreeNodeExtensionMethods
    {
        public static bool IsScreenTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is ScreenSave;
        }

        public static bool IsComponentTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is ComponentSave;
        }

        public static bool IsBehaviorTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is BehaviorSave;
        }

        public static bool IsStandardElementTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is StandardElementSave;
        }

        public static bool IsInstanceTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is InstanceSave;
        }

        public static bool IsStateSaveTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is StateSave;
        }

        public static bool IsTopElementContainerTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag == null;
        }

        public static bool IsTopScreenContainerTreeNode(this TreeNode treeNode)
        {
            return treeNode.Parent == null && treeNode.Text == "Screens";
        }

        public static bool IsTopBehaviorTreeNode(this TreeNode treeNode)
        {
            return treeNode.Parent == null && treeNode.Text == "Behaviors";
        }

        public static bool IsTopComponentContainerTreeNode(this TreeNode treeNode)
        {
            return treeNode.Parent == null && treeNode.Text == "Components";
        }

        public static bool IsTopStandardElementTreeNode(this TreeNode treeNode)
        {
            return treeNode.Parent == null && treeNode.Text == "Standard";
        }

        public static FilePath GetFullFilePath(this TreeNode treeNode)
        {
            if (treeNode.IsTopComponentContainerTreeNode() ||
                treeNode.IsTopStandardElementTreeNode() ||
                treeNode.IsTopScreenContainerTreeNode() ||
                treeNode.IsTopBehaviorTreeNode()
                )
            {
                if (ProjectManager.Self.GumProjectSave == null ||
                    string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
                {
                    MessageBox.Show("Project isn't saved yet so the root of the project isn't known");
                    return null;
                }
                else
                {
                    string projectDirectory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

                    if (treeNode.IsTopComponentContainerTreeNode())
                    {
                        return projectDirectory + ElementReference.ComponentSubfolder + "\\";
                    }
                    else if (treeNode.IsTopStandardElementTreeNode())
                    {
                        return projectDirectory + ElementReference.StandardSubfolder + "\\";
                    }
                    else if (treeNode.IsTopScreenContainerTreeNode())
                    {
                        return projectDirectory + ElementReference.ScreenSubfolder + "\\";
                    }
                    else if(treeNode.IsTopBehaviorTreeNode())
                    {
                        return projectDirectory + BehaviorReference.Subfolder + "\\";
                    }
                    throw new InvalidOperationException();
                }
            }
            else if (treeNode.IsStandardElementTreeNode() ||
                treeNode.IsComponentTreeNode() ||
                treeNode.IsScreenTreeNode())
            {
                ElementSave element = treeNode.Tag as ElementSave;
                return treeNode.Parent.GetFullFilePath() + treeNode.Text + "." + element.FileExtension;
            }
            else if(treeNode.IsBehaviorTreeNode())
            {
                var behavior = treeNode.Tag as BehaviorSave;
                return treeNode.Parent.GetFullFilePath() + treeNode.Text + "." + BehaviorReference.Extension;
            }
            else
            {
                var toReturn = treeNode.Parent.GetFullFilePath() + treeNode.Text + "\\";
                return toReturn;
            }
        }

        public static bool IsScreensFolderTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag == null &&
                treeNode.Parent != null &&
                (treeNode.Parent.IsScreensFolderTreeNode() || treeNode.Parent.IsTopScreenContainerTreeNode());
        }



        public static bool IsPartOfScreensFolderStructure(this TreeNode treeNode)
        {
            if (treeNode == ElementTreeViewManager.Self.RootScreensTreeNode)
                return true;

            if (treeNode.Parent == null)
                return false;

            return treeNode.Parent.IsPartOfScreensFolderStructure();
        }

        public static bool IsPartOfComponentsFolderStructure(this TreeNode treeNode)
        {
            if (treeNode == ElementTreeViewManager.Self.RootComponentsTreeNode)
                return true;

            if (treeNode.Parent == null)
                return false;

            return treeNode.Parent.IsPartOfComponentsFolderStructure();
        }

        public static bool IsPartOfStandardElementsFolderStructure(this TreeNode treeNode)
        {
            if (treeNode == ElementTreeViewManager.Self.RootStandardElementsTreeNode)
                return true;

            if (treeNode.Parent == null)
                return false;

            return treeNode.Parent.IsPartOfStandardElementsFolderStructure();
        }


        public static bool IsComponentsFolderTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag == null &&
                treeNode.Parent != null &&
                (treeNode.Parent.IsComponentsFolderTreeNode() || treeNode.Parent.IsTopComponentContainerTreeNode());
        }

        public static void SortByName(this TreeNodeCollection treeNodeCollection, bool recursive = false)
        {
            int lastObjectExclusive = treeNodeCollection.Count;
            int whereObjectBelongs;
            for (int i = 0 + 1; i < lastObjectExclusive; i++)
            {
                TreeNode first = treeNodeCollection[i];
                TreeNode second = treeNodeCollection[i - 1];
                if (FirstComesBeforeSecond(first, second))
                {
                    if (i == 1)
                    {
                        TreeNode treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);

                        treeNodeCollection.Insert(0, treeNode);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        second = treeNodeCollection[whereObjectBelongs];
                        if (!FirstComesBeforeSecond(treeNodeCollection[i], second))
                        {
                            TreeNode treeNode = treeNodeCollection[i];

                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && FirstComesBeforeSecond(treeNodeCollection[i], treeNodeCollection[0]))
                        {
                            TreeNode treeNode = treeNodeCollection[i];
                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(0, treeNode);
                            break;
                        }
                    }
                }
            }

            if(recursive)
            {
                foreach(var node in treeNodeCollection)
                {
                    var asTreeNode = node as TreeNode;
                    if(asTreeNode != null)
                    {
                        var sortInner = asTreeNode.IsScreenTreeNode() == false &&
                            asTreeNode.IsComponentTreeNode() == false &&
                            asTreeNode.IsStandardElementTreeNode() == false &&
                            asTreeNode.IsBehaviorTreeNode() == false;

                        if(sortInner)
                        {
                            asTreeNode.Nodes.SortByName(recursive);
                        }
                    }
                }
            }
        }

        private static bool FirstComesBeforeSecond(TreeNode first, TreeNode second)
        {
            bool isFirstDirectory = first.IsComponentsFolderTreeNode() || first.IsScreensFolderTreeNode();
            bool isSecondDirectory = second.IsComponentsFolderTreeNode() || second.IsScreensFolderTreeNode();

            if (isFirstDirectory && !isSecondDirectory)
            {
                return true;
            }
            else if (!isFirstDirectory && isSecondDirectory)
            {
                return false;
            }
            else
            {
                return first.Text.CompareTo(second.Text) < 0;
            }
        }

        public static List<TreeNode> GetAllChildrenNodesRecursively(this TreeNode treeNode)
        {
            List<TreeNode> toReturn = new List<TreeNode>();

            void Fill(TreeNode parent)
            {
                foreach(TreeNode child in parent.Nodes)
                {
                    toReturn.Add(child);
                    Fill(child);
                }
            }

            Fill(treeNode);

            return toReturn;
        }
    }

#endregion
}
