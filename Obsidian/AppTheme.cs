using MudBlazor;

namespace Obsidian
{
    public class AppTheme : MudTheme
    {
        public AppTheme()
        {
            // Configuración para el tema claro
            this.Palette = new PaletteLight
            {
                Primary = Colors.Red.Accent2,
                AppbarBackground = "#FF5733",  // Rojo anaranjado para el fondo de la barra de aplicaciones
                DrawerBackground = "#FF5733"   // Rojo anaranjado para el fondo del cajón
            };

            // Configuración para el tema oscuro
            this.PaletteDark = new PaletteDark
            {
                Primary = Colors.Red.Accent2,
                AppbarBackground = "#C70039",  // Rojo oscuro para el fondo de la barra de aplicaciones
                DrawerBackground = "#C70039"   // Rojo oscuro para el fondo del cajón
            };

            // Configuración adicional
            this.LayoutProperties = new LayoutProperties { DefaultBorderRadius = "10px" };

            this.Typography = new Typography
            {
                Default = new Default { FontFamily = new[] { "Fira Sans" } }
            };
        }
    }
}
