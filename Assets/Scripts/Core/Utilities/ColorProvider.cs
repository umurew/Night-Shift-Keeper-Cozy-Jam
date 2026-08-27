using UnityEngine;

public static class ColorProvider
{
    public static GizmoColorsClass GizmoColors { get; } = new();
    public static UIColorsClass UIColors { get; } = new();

    public class GizmoColorsClass
    {
        public Color InteractableCollider { get; } = Color.softYellow;
        public Color DoorAxis { get; } = Color.darkCyan;
        public Color DoorArrow { get; } = Color.cyan;
        public Color HandleLabel { get; } = Color.antiqueWhite;
    }

    public class UIColorsClass
    {
        public string ErrorColor => "#CC4141";
        public string PrimaryColor => "#E1E1E1";
        public string AccentColor => "#77BAFF";
        public string GrayedColor => "#8493A3";
    }
}
