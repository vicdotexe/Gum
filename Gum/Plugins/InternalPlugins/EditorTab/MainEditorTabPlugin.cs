﻿using FlatRedBall.AnimationEditorForms.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ScrollBarPlugin;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.EditorTab;

[Export(typeof(PluginBase))]
internal class MainEditorTabPlugin : InternalPlugin
{
    static HashSet<string> PropertiesSupportingIncrementalChange = new HashSet<string>
        {
            "Animate",
            "Alpha",
            "AutoGridHorizontalCells",
            "AutoGridVerticalCells",
            "Blue",
            "CurrentChainName",
            "Children Layout",
            "FlipHorizontal",
            "FontSize",
            "Green",
            "Height",
            "Height Units",
            "HorizontalAlignment",
            nameof(GraphicalUiElement.IgnoredByParentSize),
            "IsBold",
            "MaxLettersToShow",
            nameof(Text.MaxNumberOfLines),
            "Red",
            "Rotation",
            "StackSpacing",
            "Text",
            "Texture Address",
            "TextOverflowVerticalMode",
            "UseCustomFont",
            "UseFontSmoothing",
            "VerticalAlignment",
            "Visible",
            "Width",
            "Width Units",
            "X",
            "X Origin",
            "X Units",
            "Y",
            "Y Origin",
            "Y Units",
        };

    public static MainEditorTabPlugin Self
    {
        get;
        private set;
    }

    readonly ScrollbarService _scrollbarService;
    WireframeControl _wireframeControl;
    private FlowLayoutPanel _toolbarPanel;

    public MainEditorTabPlugin()
    {
        _scrollbarService = new ScrollbarService();
        Self = this;
    }

    public override void StartUp()
    {
        AssignEvents();


    }

    private void AssignEvents()
    {
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.InstanceSelected += HandleInstanceSelected;
        this.ElementSelected += HandleElementSelected;
        this.VariableSetLate += HandleVariableSetLate;

        this.CameraChanged += _scrollbarService.HandleCameraChanged;
        this.XnaInitialized += HandleXnaInitialized;
        this.WireframeResized += _scrollbarService.HandleWireframeResized;
        this.ElementSelected += _scrollbarService.HandleElementSelected;
    }

    private void HandleVariableSetLate(ElementSave element, InstanceSave instance, string qualifiedName, object oldValue)
    {
        var state = SelectedState.Self.SelectedStateSave ?? element.DefaultState;

        if(instance != null)
        {
            qualifiedName = instance.Name + "." + qualifiedName;
        }

        var value = state.GetValue(qualifiedName);

        var areSame = value == null && oldValue == null;
        if (!areSame && value != null)
        {
            areSame = value.Equals(oldValue);
        }

        // If the values are the same they may have been set to be the same by a plugin that
        // didn't allow the assignment, so don't go through the work of saving and refreshing
        if (!areSame)
        {
            var unqualifiedMember = qualifiedName;
            if(qualifiedName.Contains("."))
            {
                unqualifiedMember = qualifiedName.Substring(qualifiedName.LastIndexOf('.') + 1);
            }

            // Inefficient but let's do this for now - we can make it more efficient later
            // November 19, 2019
            // While this is inefficient
            // at runtime, it is *really*
            // inefficient for debugging. If
            // a set value fails, we have to trace
            // the entire variable assignment and that
            // can take forever. Therefore, we're going to
            // migrate towards setting the individual values
            // here. This can expand over time to just exclude
            // the RefreshAll call completely....but I don't know
            // if that will cause problems now, so instead I'm going
            // to do it one by one:
            var handledByDirectSet = false;

            // if a deep reference is set, then this is more complicated than a single variable assignment, so we should
            // force everything. This makes debugging a little more difficult, but it keeps the wireframe accurate without having to track individual assignments.
            if (PropertiesSupportingIncrementalChange.Contains(unqualifiedMember) &&
            // June 19, 2024 - if the value is null (from default assignment), we
            // can't set this single value - it requires a recursive variable finder.
            // for simplicity (for now?) we will just refresh all:
                value != null &&

                (instance != null || SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null))
            {
                // this assumes that the object having its variable set is the selected instance. If we're setting
                // an exposed variable, this is not the case - the object having its variable set is actually the instance.
                //GraphicalUiElement gue = WireframeObjectManager.Self.GetSelectedRepresentation();
                GraphicalUiElement gue = null;
                if (instance != null)
                {
                    gue = WireframeObjectManager.Self.GetRepresentation(instance);
                }
                else
                {
                    gue = WireframeObjectManager.Self.GetSelectedRepresentation();
                }

                if (gue != null)
                {
                    gue.SetProperty(unqualifiedMember, value);

                    WireframeObjectManager.Self.RootGue?.ApplyVariableReferences(SelectedState.Self.SelectedStateSave);
                    //gue.ApplyVariableReferences(SelectedState.Self.SelectedStateSave);

                    handledByDirectSet = true;
                }
                if (unqualifiedMember == "Text" && LocalizationManager.HasDatabase)
                {
                    WireframeObjectManager.Self.ApplyLocalization(gue, value as string);
                }
            }

            if (!handledByDirectSet)
            {
                WireframeObjectManager.Self.RefreshAll(true, forceReloadTextures: false);
            }


            SelectionManager.Self.Refresh();
        }
    }

    // todo - When a new element is selected, a new state is selected too
    // need to only handle this 1 time. Currently there is a double-refresh
    private void HandleElementSelected(ElementSave save)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: true);
    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: false);
    }

    private void HandleXnaInitialized()
    {
        _scrollbarService.HandleXnaInitialized();


        this._wireframeControl.Parent.Resize += (not, used) =>
        {
            UpdateWireframeControlSizes();
            PluginManager.Self.HandleWireframeResized();
        };

        this._wireframeControl.MouseClick += wireframeControl1_MouseClick;

        this._wireframeControl.DragDrop += DragDropManager.Self.HandleFileDragDrop;
        this._wireframeControl.DragEnter += DragDropManager.Self.HandleFileDragEnter;
        this._wireframeControl.DragOver += (sender, e) =>
        {
            //this.DoDragDrop(e.Data, DragDropEffects.Move | DragDropEffects.Copy);
            //DragDropManager.Self.HandleDragOver(sender, e);

        };

        // December 29, 2024
        // AppCenter is dead - do we want to replace this?
        //_wireframeControl.ErrorOccurred += (exception) => Crashes.TrackError(exception);

        this._wireframeControl.QueryContinueDrag += (sender, args) =>
        {
            args.Action = DragAction.Continue;
        };
        _wireframeControl.CameraChanged += () =>
        {
            PluginManager.Self.CameraChanged();
        };

        this._wireframeControl.KeyDown += (o, args) =>
        {
            if (args.KeyCode == Keys.Tab)
            {
                GumCommands.Self.GuiCommands.ToggleToolVisibility();
            }
        };

        // Apply FrameRate, but keep it within sane limits
        float frameRate = Math.Max(Math.Min(ProjectManager.Self.GeneralSettingsFile.FrameRate, 60), 10);
        _wireframeControl.DesiredFramesPerSecond = frameRate;

        UpdateWireframeControlSizes();
    }

    public void HandleWireframeInitialized(WireframeControl wireframeControl1, WireframeEditControl wireframeEditControl, 
        System.Windows.Forms.Cursor addCursor, System.Windows.Forms.Panel gumEditorPanel, FlowLayoutPanel toolbarPanel)
    {
        GumCommands.Self.GuiCommands.AddControl(gumEditorPanel, "Editor", TabLocation.RightTop);


        _wireframeControl = wireframeControl1;
        _toolbarPanel = toolbarPanel;
        wireframeControl1.XnaUpdate += () =>
        {
            Wireframe.WireframeObjectManager.Self.Activity();
            ToolLayerService.Self.Activity();
        };

        _scrollbarService.HandleWireframeInitialized(wireframeControl1, gumEditorPanel);

        ToolCommands.GuiCommands.Self.Initialize(wireframeControl1);

        Wireframe.WireframeObjectManager.Self.Initialize(wireframeEditControl, wireframeControl1, addCursor);
        wireframeControl1.Initialize(wireframeEditControl, gumEditorPanel, HotkeyManager.Self);

    }

    /// <summary>
    /// Refreshes the wifreframe control size - for some reason this is necessary if windows has a non-100% scale (for higher resolution displays)
    /// </summary>
    private void UpdateWireframeControlSizes()
    {
        // I don't think we need this for docking:
        //WireframeEditControl.Width = WireframeEditControl.Parent.Width / 2;

        //_toolbarPanel.Width = _toolbarPanel.Parent.Width;

        _wireframeControl.Width = _wireframeControl.Parent.Width;

        // Add location.Y to account for the shortcut bar at the top.
        _wireframeControl.Height = _wireframeControl.Parent.Height - _wireframeControl.Location.Y;
    }

    private void HandleStateSelected(StateSave save)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: true);
    }

    private void wireframeControl1_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            EditingManager.Self.OnRightClick();
        }
    }
}
