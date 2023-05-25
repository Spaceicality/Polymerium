using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Polymerium.App.Controls;

public class RecentPlayedItemControl : ButtonBase
{
    private Border? PART_Background;
    private Border? PART_Border;

    protected override void OnApplyTemplate()
    {
        PART_Background = GetTemplateChild("PART_Background") as Border;
        PART_Border = GetTemplateChild("PART_Border") as Border;
        base.OnApplyTemplate();
    }

    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        PART_Background!.Opacity = 1.0d;
        PART_Border!.Opacity = 0.0d;
        base.OnPointerEntered(e);
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        PART_Background!.Opacity = 0.0d;
        PART_Border!.Opacity = 1.0d;
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        PART_Background!.Opacity = 0.5d;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        PART_Background!.Opacity = IsPointerOver ? 1.0d : 0d;
        base.OnPointerReleased(e);
    }
}
