using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using RenderingLibrary;

namespace TestProject1;

public class UnitTest1
{
    static UnitTest1()
    {
        // class-level setup
        SystemManagers.Default = new();
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        FormsUtilities.InitializeDefaults();
    }

    [Fact]
    public void SetBindingToBindingContext_ShouldProperlyBindToChild()
    {
        // Arrange
        var stackPanel = new StackPanel();
        var vm = new TestViewModel { Text = "Test 1243" };
        var child = new TestViewModel { Text = "Child 1243" };
        vm.Child = child;

        stackPanel.BindingContext = vm;

        var textBox = new TextBox();
        textBox.SetBinding(nameof(TextBox.BindingContext), nameof(TestViewModel.Child));
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));
        stackPanel.AddChild(textBox);

        // Assert
        Assert.Equal(child, textBox.BindingContext);
        Assert.Equal("Child 1243", textBox.Text);
    }

    private class TestViewModel : ViewModel
    {
        public TestViewModel? Child
        {
            get => Get<TestViewModel?>();
            set => Set(value);
        }

        public string? Text
        {
            get => Get<string?>();
            set => Set(value);
        }

        public float FloatValue
        {
            get => Get<float>();
            set => Set(value);
        }

        public bool BoolValue
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}