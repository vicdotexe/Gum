﻿using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gum.Plugins.AlignmentButtons
{
    /// <summary>
    /// Interaction logic for AlignmentControl.xaml
    /// </summary>
    public partial class DockControl : UserControl
    {
        public DockControl()
        {
            InitializeComponent();
        }

        private void TopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(SelectedState.Self.SelectedInstance != null)
            {
                var instance = SelectedState.Self.SelectedInstance;

                var state = SelectedState.Self.SelectedStateSave;

                string instancePrefix = instance.Name + ".";

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Top, PositionUnitType.PixelsFromTop);

                state.SetValue(instancePrefix + "Width", 0.0f, "float");
                state.SetValue(instancePrefix + "Width Units",
                    DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


                RefreshAndSave();
            }
        }


        private void LeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                if (SelectedState.Self.SelectedInstance != null)
                {
                    var instance = SelectedState.Self.SelectedInstance;

                    var state = SelectedState.Self.SelectedStateSave;

                    string instancePrefix = instance.Name + ".";

                    SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Left, PositionUnitType.PixelsFromLeft);
                    SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                    state.SetValue(instancePrefix + "Height", 0.0f, "float");
                    state.SetValue(instancePrefix + "Height Units",
                        DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                    RefreshAndSave();
                }
            }
        }

        private void FillButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                var instance = SelectedState.Self.SelectedInstance;

                var state = SelectedState.Self.SelectedStateSave;

                string instancePrefix = instance.Name + ".";

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                state.SetValue(instancePrefix + "Width", 0.0f, "float");
                state.SetValue(instancePrefix + "Width Units",
                    DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);

                state.SetValue(instancePrefix + "Height", 0.0f, "float");
                state.SetValue(instancePrefix + "Height Units",
                    DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


                RefreshAndSave();
            }
        }

        private void RightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                var instance = SelectedState.Self.SelectedInstance;

                var state = SelectedState.Self.SelectedStateSave;

                string instancePrefix = instance.Name + ".";

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Right, PositionUnitType.PixelsFromRight);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

                state.SetValue(instancePrefix + "Height", 0.0f, "float");
                state.SetValue(instancePrefix + "Height Units",
                    DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);



                RefreshAndSave();
            }
        }

        private void BottomButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                var instance = SelectedState.Self.SelectedInstance;

                var state = SelectedState.Self.SelectedStateSave;

                string instancePrefix = instance.Name + ".";

                SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
                SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Bottom, PositionUnitType.PixelsFromBottom);

                state.SetValue(instancePrefix + "Width", 0.0f, "float");
                state.SetValue(instancePrefix + "Width Units",
                    DimensionUnitType.RelativeToContainer, typeof(DimensionUnitType).Name);


                RefreshAndSave();
            }
        }

        private void SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment alignment, PositionUnitType xUnits)
        {
            var state = SelectedState.Self.SelectedStateSave;
            var instance = SelectedState.Self.SelectedInstance;
            string instancePrefix = instance.Name + ".";


            state.SetValue(instancePrefix + "X", 0.0f, "float");
            state.SetValue(instancePrefix + "X Origin",
                alignment, "HorizontalAlignment");
            state.SetValue(instancePrefix + "X Units",
               xUnits, typeof(Gum.Managers.PositionUnitType).Name);

            if (instance != null && instance.BaseType == "Text")
            {
                state.SetValue(instancePrefix + "HorizontalAlignment", alignment, "HorizontalAlignment");
            }

        }


        private void SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment alignment, PositionUnitType yUnits)
        {
            var state = SelectedState.Self.SelectedStateSave;
            var instance = SelectedState.Self.SelectedInstance;
            string instancePrefix = instance.Name + ".";

            state.SetValue(instancePrefix + "Y", 0.0f, "float");
            state.SetValue(instancePrefix + "Y Origin",
                alignment, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
            state.SetValue(instancePrefix + "Y Units",
                yUnits, typeof(PositionUnitType).Name);

            if (instance != null && instance.BaseType == "Text")
            {
                state.SetValue(instancePrefix + "VerticalAlignment", alignment, "VerticalAlignment");
            }

        }

        private static void RefreshAndSave()
        {
            GumCommands.Self.GuiCommands.RefreshPropertyGrid();
            GumCommands.Self.WireframeCommands.Refresh();
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }
    }
}
