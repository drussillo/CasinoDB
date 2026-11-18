using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

// This was added to prevent code duplication when designing UI elements

namespace CasinoApp
{
    public static class UIHelper
    {
        /* Used for when you want to create a stack panel when creating a dialog box */
        public static StackPanel CreateFormPanel(params UIElement[] controls)
        {
            var panel = new StackPanel { Spacing = 12 };

            foreach (var control in controls)
            {
                panel.Children.Add(control);
            }

            return panel;
        }

        public static ContentDialog CreateDialog(string title, object content, string primaryButton, string closeButton, XamlRoot root)
        {
            return new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButton,
                CloseButtonText = closeButton,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = root
            };
        }

        public static async Task ShowErrorAsync(XamlRoot root, string errmessage = "None Provided")
        {
            var message = new TextBlock
            {
                Text = "The database operation failed. Check the database restrictions and try again.\nAdditonal Information:\n" + errmessage,
                TextWrapping = TextWrapping.Wrap
            };

            var dialog = UIHelper.CreateDialog("Database Error", message, "Close", "", root);
            await dialog.ShowAsync();
        }
    }
}
