﻿using Gum.Wireframe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
namespace MonoGameGum.Forms.Controls;
#endif

public class ItemsControl : ScrollViewer
{
    #region Fields/Properties

    protected Type ItemGumType { get; set; }

    Type itemFormsType = typeof(ListBoxItem);


    // There can be a logical conflict when dealing with list items.
    // When creating a Gum list item, the Gum object may specify a Forms
    // type. But the list can also specify a forms type. So which do we use?
    // We'll use the list item forms type unless the list box has its value set
    // explicitly. then we'll go to the list box type. This eventually should get
    // marked as obsolete and we should instead go to a VM solution.
    protected bool isItemTypeSetExplicitly = false;
    protected Type ItemFormsType
    {
        get => itemFormsType;
        set
        {
            if (value != itemFormsType)
            {
                isItemTypeSetExplicitly = true;
                itemFormsType = value;
            }
        }
    }

    IList items;
    public IList Items
    {
        get => items;
        set
        {
            if (items != value)
            {
                if (items != null)
                {
                    ClearVisualsInternal();
                }

                if (items is INotifyCollectionChanged notifyCollectionChanged)
                {
                    notifyCollectionChanged.CollectionChanged -= HandleCollectionChanged;
                }
                items = value;
                if (items is INotifyCollectionChanged newNotifyCollectionChanged)
                {
                    newNotifyCollectionChanged.CollectionChanged += HandleCollectionChanged;
                }

                if (items?.Count > 0)
                {
                    // refresh!
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                        items, startingIndex: 0);
                    HandleCollectionChanged(this, args);
                }
            }
        }
    }



    public FrameworkElementTemplate FrameworkElementTemplate { get; set; }

    VisualTemplate visualTemplate;
    public VisualTemplate VisualTemplate
    {
        get => visualTemplate;
        set
        {
            if (value != visualTemplate)
            {
                visualTemplate = value;

                if (items != null)
                {
                    ClearVisualsInternal();

                    if (items.Count > 0)
                    {
                        // refresh!
                        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, startingIndex: 0);
                        HandleCollectionChanged(this, args);
                    }
                }
            }
        }
    }

    public event EventHandler<NotifyCollectionChangedEventArgs> ItemsCollectionChanged;

    #endregion

    #region Events

    public event EventHandler ItemClicked;

    #endregion

    public ItemsControl() : base()
    {
        Items = new ObservableCollection<object>();
    }

    public ItemsControl(InteractiveGue visual) : base(visual)
    {
        Items = new ObservableCollection<object>();
    }

    protected virtual FrameworkElement CreateNewItemFrameworkElement(object o)
    {
        var label = new Label();
        label.Text = o?.ToString();
        label.BindingContext = o;
        return label;
    }


    protected virtual InteractiveGue CreateNewVisual(object vm)
    {
        if (VisualTemplate != null)
        {
            return VisualTemplate.CreateContent(vm) as InteractiveGue;
        }
        else
        {
            var listBoxItemGumType = ItemGumType;

            if (listBoxItemGumType == null && DefaultFormsComponents.ContainsKey(typeof(ListBoxItem)))
            {
                listBoxItemGumType = DefaultFormsComponents[typeof(ListBoxItem)];
            }
#if DEBUG
            if (listBoxItemGumType == null)
            {
                throw new Exception($"This {GetType().Name} named {this.Name} does not have a ItemGumType specified, nor does the DefaultFormsComponents have an entry for ListBoxItem. " +
                    "This property must be set before adding any items");
            }
#endif
            // vic says - this uses reflection, could be made faster, somehow...

            var gumConstructor = listBoxItemGumType.GetConstructor(new[] { typeof(bool), typeof(bool) });
            var visual = gumConstructor.Invoke(new object[] { true, true }) as InteractiveGue;
            return visual;
        }
    }

    #region Event Handler methods

    protected virtual void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    int index = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                    {
                        var newItem = CreateNewItemFrameworkElement(item);

                        InnerPanel.Children.Insert(index, newItem.Visual);

                        newItem.Visual.Parent = base.InnerPanel;
                        HandleCollectionNewItemCreated(newItem, index);

                        index++;
                    }
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    var index = e.OldStartingIndex;

                    var listItem = InnerPanel.Children[index];
                    HandleCollectionItemRemoved(index);
                    listItem.Parent = null;
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                ClearVisualsInternal();
                HandleCollectionReset();
                break;
            case NotifyCollectionChangedAction.Replace:
                {
                    var index = e.NewStartingIndex;
                    var listItem = InnerPanel.Children[index];
                    HandleCollectionReplace(index);
                    
                }

                break;
        }

        ItemsCollectionChanged?.Invoke(sender, e);
    }

    protected virtual void HandleCollectionNewItemCreated(FrameworkElement newItem, int newItemIndex) { }
    protected virtual void HandleCollectionItemRemoved(int inexToRemoveFrom) { }
    protected virtual void HandleCollectionReset() { }
    protected virtual void HandleCollectionReplace(int index) { }

    private void ClearVisualsInternal()
    {
        for (int i = InnerPanel.Children.Count - 1; i > -1; i--)
        {
            InnerPanel.Children[i].Parent = null;
        }
    }



    protected void OnItemClicked(object sender, EventArgs args)
    {
        ItemClicked?.Invoke(sender, args);
    }

    #endregion

    #region Update To

#if FRB
    protected override void HandleVisualBindingContextChanged(object sender, BindingContextChangedEventArgs args)
    {
        if(args.OldBindingContext != null && BindingContext == null)
        {
            // user removed the binding context, usually this happens when the object is removed
            if(vmPropsToUiProps.ContainsValue(nameof(Items)))
            {
                // null out the items!
                this.Items = null;
            }
        }
        base.HandleVisualBindingContextChanged(sender, args);
    }
#endif
    #endregion
}
