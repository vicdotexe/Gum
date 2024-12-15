# Gum Forms

### Introduction

Gum Forms provides a collection of standardized, fully functional UI elements. MonoGame Gum includes the following types:

* Button
* CheckBox
* ComboBox
* ListBox
* ListBoxItem (used by ListBox)
* PasswordBox
* RadioButton
* ScrollView
* Slider&#x20;
* TextBox

We can use all of the types above by adding instances of components which map to these controls.

{% hint style="info" %}
This tutorial does not require any any of the instances from the previous tutorial. It assumes that you still have a Gum project and that you have set up your Game class to include the necessary Initialize, Draw, and Update calls.

If you would like a simpler starting point, feel free to delete all content in your TitleScreen in Gum, and feel free to delete all code aside from the bare minimum for your project.

In other words, you can reset your game screen to be as shown in the following code:

```csharp
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    GraphicalUiElement Root;
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        var gumProject = MonoGameGum.GumService.Default.Initialize(
            this.GraphicsDevice,
            // This is relative to Content:
            "GumProject/GumProject.gumx");

        Root = screen.ToGraphicalUiElement(
            RenderingLibrary.SystemManagers.Default, addToManagers: true);

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        MonoGameGum.GumService.Default.Update(this, gameTime, Root);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        MonoGameGum.GumService.Default.Draw();
        base.Draw(gameTime);
    }
}

```
{% endhint %}



### Adding Forms Instances to a Screen

The previous tutorial showed how to add a Button instance to our screen. We can add other functional controls by drag+dropping instances into the TitleScreen. This tutorial shows how to interact with a ListBox, so you should drag+drop a ListBox instance into your screen. You can also add additional instances of other types if you would like to see them in action, such as CheckBox, ComboBox, Slider, and TextBox.

<figure><img src="../../../.gitbook/assets/24_12 02 10.gif" alt=""><figcaption><p>Drag+dropping Forms components into the TitleScreen</p></figcaption></figure>

Our forms controls already have some functionality even before we write any code in our game.

<figure><img src="../../../.gitbook/assets/24_12 03 46.gif" alt=""><figcaption><p>Forms controls with built-in functionality</p></figcaption></figure>

### Interacting with Forms Instances

We can interact with any of the Forms instances by using `GetFrameworkElementByName`. For example, to interact with the ListBox that we added in the previous section, add the following code to your Initialize method to add items to the ListBox:

```csharp
protected override void Initialize()
{
    var gumProject = MonoGameGum.GumService.Default.Initialize(
        this.GraphicsDevice,
        // This is relative to Content:
        "GumProject/GumProject.gumx");      
        
    var screen = gumProject.Screens.Find(item => item.Name == "TitleScreen");
        
    Root = screen.ToGraphicalUiElement(
        RenderingLibrary.SystemManagers.Default, addToManagers: true);

// Start of new code
    var listBox = Root.GetFrameworkElementByName<ListBox>("ListBoxInstance");
    for(int i = 0; i < 50; i++)
    {
        listBox.Items.Add("Item number " + i.ToString());
    }
// End of new code

    base.Initialize();
}
```

<figure><img src="../../../.gitbook/assets/24_12 10 48.gif" alt=""><figcaption><p>ListBox with 50 items</p></figcaption></figure>

Forms types such as Button are associated with Gum components based on their category. For example, the following components can be used to create Button instances.

<figure><img src="../../../.gitbook/assets/image (105).png" alt=""><figcaption><p>Multiple components create Button forms controls</p></figcaption></figure>

Although the prefix "Button" suggests that these controls are Forms Buttons, the name can change and these would still create buttons. At runtime the type of Forms control associated with a component is determined by the state categories defined in the component.

For example, each of these components has a state category named ButtonCategory.

<figure><img src="../../../.gitbook/assets/image (106).png" alt=""><figcaption><p>ButtonClose with a ButtonCategory state category</p></figcaption></figure>

Although we won't cover the details in this tutorial, you can customize the existing components or create new components which will map to the Forms types so long as they have the appropriate category.

### Additional Documentation

Forms component instances can be added and modified just like any other instance, but at runtime these types provide common properties and methods. To learn more about working with Forms in code, see the [Forms documentation](../../gum-forms/controls/).

{% hint style="info" %}
The Forms types and properties are based on the WPF syntax. Developers familiar with WPF may find that many of the same members exist in Gum Forms. However, keep in mind that Gum Forms are still using Gum for the layout engine, so any properties related to position or size follow the Gum rules rather than WPF rules.
{% endhint %}

### Conclusion

This tutorial showed how to create Forms instances in a screen, interact with them in code, and how to work with the different forms types.

The next tutorial covers how to generate code for custom components.