using System.Linq;
using Microsoft.Maui.Controls;

namespace SistemasDeGestionCitasPeluqueria.Behaviors
{
    public sealed class DigitsOnlyBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnTextChanged;
            base.OnDetachingFrom(entry);
        }

        private static void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry) return;
            var txt = entry.Text;
            if (string.IsNullOrEmpty(txt)) return;

            var filtered = new string(txt.Where(char.IsDigit).ToArray());
            if (filtered != txt)
                entry.Text = filtered; // elimina letras o símbolos
        }
    }
}