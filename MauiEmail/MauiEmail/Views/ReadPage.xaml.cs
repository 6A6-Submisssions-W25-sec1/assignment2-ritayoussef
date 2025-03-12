using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MauiEmail.Models;

namespace MauiEmail.Views;

public partial class ReadPage : ContentPage
{
    private ObservableMessage _email;

    public ReadPage(ObservableMessage email)
    {
        InitializeComponent();
        _email = email;
        BindingContext = _email;
        MarkEmailAsRead(_email);
        LoadEmailData(email);
    }

    private async void MarkEmailAsRead(ObservableMessage email)
    {
        if (email.UniqueId.HasValue)
        {
            await App.EmailService.MarkAsReadAsync(email.UniqueId.Value);
            email.IsRead = true;
        }
    }

    private async void LoadEmailData(ObservableMessage email)
    {
        try
        {
            await App.EmailService.MarkAsReadAsync(email.UniqueId.Value);
            email.IsRead = true;
            
            await Task.Delay(500);

            var fullEmail = await App.EmailService.DownloadEmailAsync(email.UniqueId.Value);
            email.HtmlBody = fullEmail?.HtmlBody ?? "No content available";
           
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading email: {ex.Message}");
        }
    }

    private async void OnForwardClicked(object sender, EventArgs e)
    {
        var forwardEmail = _email.GetForward();
       // await Navigation.PushAsync(new WritePage(forwardEmail));
    }
}
