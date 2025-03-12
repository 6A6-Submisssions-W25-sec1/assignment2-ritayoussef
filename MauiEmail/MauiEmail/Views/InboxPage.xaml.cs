using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EmailConsoleApp;
using MauiEmail.Models;

 

namespace MauiEmail.Views;

    public partial class InboxPage : ContentPage
    {
        public ObservableCollection<ObservableMessage> Emails { get; set; }
        private ObservableCollection<ObservableMessage> _allEmails;
        public ICommand DeleteCommand { get; set; }
        public ICommand FavoriteCommand { get; set; }
        public ICommand SearchCommand { get; set; }
        public ICommand EmailTappedCommand { get; set; }

        public InboxPage()
        {
            InitializeComponent();
            DownloadEmails();
            BindingContext = this;
          
            
            
            Emails = new ObservableCollection<ObservableMessage>();
            DeleteCommand = new Command<ObservableMessage>(DeleteEmail);
            FavoriteCommand = new Command<ObservableMessage>(ToggleFavorite);
            SearchCommand = new Command<string>(SearchEmails);
            EmailTappedCommand = new Command<ObservableMessage>(OnEmailTapped); 

            Shell.SetTitleView(this, CreateSearchBar());
        }

        private View CreateSearchBar()
        {
            var searchBar = new SearchBar
            {
                Placeholder = "Search emails...",
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            searchBar.TextChanged += (s, e) => SearchEmails(searchBar.Text);
            return searchBar;
        }

        private async void DownloadEmails()
        {
            var messages = await App.EmailService.FetchAllMessages();

            if (messages == null || !messages.Any())
            {
                Console.WriteLine("LoadEmails: No emails found!");
                _allEmails = new ObservableCollection<ObservableMessage>();
                return;
            }

            _allEmails = new ObservableCollection<ObservableMessage>(messages);
            Emails.Clear();

            foreach (var msg in _allEmails)
            {
                Console.WriteLine($"LoadEmails: Email from {msg.From.Name}, Initial: {msg.SenderInitial}, UID: {msg.UniqueId}");
                Emails.Add(msg);
            }
            
            OnPropertyChanged(nameof(Emails)); 
            
            Console.WriteLine("Emails Loaded & UI Updated");
        }


        
        public void FilterEmails(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                Emails = new ObservableCollection<ObservableMessage>(_allEmails);
            }
            else
            {
                Emails = new ObservableCollection<ObservableMessage>(
                    _allEmails.Where(email =>
                        email.Subject.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        email.From.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    )
                );
            }
            OnPropertyChanged(nameof(Emails));
        }


        private async void DeleteEmail(ObservableMessage email)
        {
            if (email == null || !email.UniqueId.HasValue || email.UniqueId.Value.Id == 0)
            {
                Console.WriteLine($"DeleteEmail: Invalid email UID ({email?.UniqueId?.Id ?? 0}). Cannot delete.");
                return;
            }

            try
            {
                Console.WriteLine($"DeleteEmail: Deleting email with UID {email.UniqueId}");
                await App.EmailService.DeleteMessageAsync(email.UniqueId.Value);

                Emails.Remove(email);
                Console.WriteLine("DeleteEmail: Email deleted successfully.");
 
                OnPropertyChanged(nameof(Emails));
                await Task.Delay(500);
                DownloadEmails();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting email: {ex.Message}");
            }
        }


        private void OnDeleteSwipe(object sender, EventArgs e)
        {
            var swipe = sender as SwipeItem;
            if (swipe?.BindingContext is ObservableMessage email)
            {
                DeleteEmail(email);
            }
        }


        private async void ToggleFavorite(ObservableMessage email)
        {
            if (email == null) return;

            try
            {
                email.IsFavorite = !email.IsFavorite;
                await App.EmailService.MarkAsFavoriteAsync(email.UniqueId.Value); 

                Console.WriteLine($"ToggleFavorite: Email {email.UniqueId} is now {(email.IsFavorite ? "Favorited" : "Unfavorited")}");
                OnPropertyChanged(nameof(Emails)); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking email as favorite: {ex.Message}");
            }
        }
        
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var query = e.NewTextValue;

            if (string.IsNullOrWhiteSpace(query))
            {
                Emails = new ObservableCollection<ObservableMessage>(_allEmails);
            }
            else
            {
                Emails = new ObservableCollection<ObservableMessage>(
                    _allEmails.Where(email =>
                        email.Subject.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        email.From.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    ));
            }

            OnPropertyChanged(nameof(Emails));
        }

        private string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged();
                    SearchEmails(_searchQuery);
                }
            }
        }

        private void SearchEmails(string query)
        {
            if (_allEmails == null || !_allEmails.Any()) 
            {
                Console.WriteLine("SearchEmails: _allEmails is null or empty, returning.");
                return;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                Emails.Clear();
                foreach (var email in _allEmails)
                {
                    Emails.Add(email);
                }
            }
            else
            {
                var filteredEmails = _allEmails
                    .Where(email =>
                        email.Subject?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                        email.From?.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                Emails.Clear();
                foreach (var email in filteredEmails)
                {
                    Emails.Add(email);
                }
            }

            OnPropertyChanged(nameof(Emails));
        }

       


        private async void OnEmailTapped(ObservableMessage email)
        {
            if (email == null) return;

            email.IsRead = true;
            await App.EmailService.MarkAsReadAsync(email.UniqueId.Value);
            Console.WriteLine($"EmailTapped: {email.Subject} marked as read.");

            await Navigation.PushAsync(new ReadPage(email));
        }

    }

