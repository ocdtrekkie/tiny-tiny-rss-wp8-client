using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TinyTinyRSSInterface;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using TinyTinyRSS.Classes;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;
using TinyTinyRSS.Resources;
using TinyTinyRSSInterface.Classes;
using Microsoft.Phone.Tasks;

namespace TinyTinyRSS
{
    public partial class ArticlePage : PhoneApplicationPage
    {
        private int feedId;
        private ObservableCollection<WrappedArticle> ArticlesCollection;
        private int TotalCount;
        private ApplicationBarIconButton toogleReadAppBarButton, toggleStarAppBarButton, openExtAppBarButton;
        private ApplicationBarMenuItem publishAppBarMenu; //, archiveAppBarMenu;

        public ArticlePage()
        {
            InitializeComponent();
            Counter.Width = ResolutionHelper.GetWidthForOrientation(Orientation);
            ArticlesCollection = new ObservableCollection<WrappedArticle>();
            BuildLocalizedApplicationBar();
            this.Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            PivotControl.DataContext = ArticlesCollection;            
            List<Headline> headlines = await TtRssInterface.getInterface().getHeadlines(feedId);
            if (headlines.Count == 0)
            {
                MessageBox.Show("No Articles found here.");
                NavigationService.GoBack();
            }
            else
            {
                TotalCount = headlines.Count;
                headlines.ForEach(x => ArticlesCollection.Add(new WrappedArticle(x)));
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string feed = "";
            if (NavigationContext.QueryString.TryGetValue("feed", out feed))
            {
                feedId = int.Parse(feed);
            }
        }

        private async void PivotControl_LoadingPivotItem(object sender, PivotItemEventArgs e)
        {
            SystemTray.ProgressIndicator.IsIndeterminate = true;
            int selectedIndex = PivotControl.SelectedIndex;
            Counter.Text = (selectedIndex + 1) + "/" + TotalCount;
            WrappedArticle item = ArticlesCollection[selectedIndex];
            if (item.Article == null)
            {
                Article article = await TtRssInterface.getInterface().getArticle(item.Headline.id);
                item.Article = article;
            }
            setHtml(item.Article.content);
            UpdateLocalizedApplicationBar(item.Article);
            SystemTray.ProgressIndicator.IsIndeterminate = false;
            if (ConnectionSettings.getInstance().markRead && item.Article != null && item.Article.unread)
            {
                bool success = await TtRssInterface.getInterface().updateArticle(item.Article.id, UpdateField.Unread, UpdateMode.False);
                if (success)
                {
                    item.Article.unread = false;
                    UpdateLocalizedApplicationBar(item.Article);
                }
            }  
        } 

        private void setHtml(string content)
        {
            PivotItem myPivotItem =
                (PivotItem)(PivotControl.ItemContainerGenerator.ContainerFromItem(PivotControl.Items[PivotControl.SelectedIndex]));

            var wc = FindDescendantByName(myPivotItem, "WebContent") as WebBrowser;
            if (wc != null)
            {
                wc.NavigateToString(content);
            }
        }

        public static FrameworkElement FindDescendantByName(FrameworkElement element, string name)
        {
            if (element == null || string.IsNullOrWhiteSpace(name)) { return null; }

            if (name.Equals(element.Name, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var result = FindDescendantByName((VisualTreeHelper.GetChild(element, i) as FrameworkElement), name);
                if (result != null) { return result; }
            }
            return null;
        }

        private void UpdateLocalizedApplicationBar(Article article)
        {
            if (article.unread)
            {
                toogleReadAppBarButton.IconUri = new Uri("/Assets/AppBar/mail-read.png", UriKind.Relative);
                toogleReadAppBarButton.Text = AppResources.MarkReadAppBarButtonText;
            }
            else
            {
                toogleReadAppBarButton.IconUri = new Uri("/Assets/AppBar/mail-unread.png", UriKind.Relative);
                toogleReadAppBarButton.Text = AppResources.MarkUnreadAppBarButtonText;
            }

            if (!article.marked)
            {
                toggleStarAppBarButton.IconUri = new Uri("/Assets/AppBar/star.png", UriKind.Relative);
                toggleStarAppBarButton.Text = AppResources.StarAppBarButtonText;
            }
            else
            {
                toggleStarAppBarButton.IconUri = new Uri("/Assets/AppBar/unstar.png", UriKind.Relative);
                toggleStarAppBarButton.Text = AppResources.UnStarAppBarButtonText;
            }
        }

        private void BuildLocalizedApplicationBar()
        {
            // ApplicationBar der Seite einer neuen Instanz von ApplicationBar zuweisen
            ApplicationBar = new ApplicationBar();

            toogleReadAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/mail-read.png", UriKind.Relative));
            toogleReadAppBarButton.Text = AppResources.MarkReadAppBarButtonText;
            toogleReadAppBarButton.Click += AppBarButton_Click;
            ApplicationBar.Buttons.Add(toogleReadAppBarButton);

            toggleStarAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/star.png", UriKind.Relative));
            toggleStarAppBarButton.Text = AppResources.StarAppBarButtonText;
            toggleStarAppBarButton.Click += AppBarButton_Click;
            ApplicationBar.Buttons.Add(toggleStarAppBarButton);

            openExtAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/globe.png", UriKind.Relative));
            openExtAppBarButton.Text = AppResources.OpenExtAppBarButtonText;
            openExtAppBarButton.Click += openExt_Click;
            ApplicationBar.Buttons.Add(openExtAppBarButton);

            //archiveAppBarMenu = new ApplicationBarMenuItem();
            //archiveAppBarMenu.Text = AppResources.ToogleArchiveAppBarButtonText;
            //archiveAppBarMenu.Click += AppBarButton_Click;
            //ApplicationBar.MenuItems.Add(archiveAppBarMenu);

            publishAppBarMenu = new ApplicationBarMenuItem();
            publishAppBarMenu.Text = AppResources.TooglePublishAppBarButtonText;
            publishAppBarMenu.Click += AppBarButton_Click;
            ApplicationBar.MenuItems.Add(publishAppBarMenu);
        }

        private async void AppBarButton_Click(object sender, EventArgs e)
        {
            UpdateField field;
            Article current = ArticlesCollection[PivotControl.SelectedIndex].Article;
            bool remove = false;
            //if (sender == archiveAppBarMenu)
            //{
            //    field = UpdateField.Archived;
            //}
            //else 
            if (sender == publishAppBarMenu)
            {
                field = UpdateField.Published;
                if (feedId == (int) FeedId.Published)
                {
                    remove = true;
                }
            }            
            else if (sender == toggleStarAppBarButton)
            {
                field = UpdateField.Starred;
                if (feedId == (int)FeedId.Starred)
                {
                    remove = true;
                }
            }
            else if (sender == toogleReadAppBarButton)
            {
                field = UpdateField.Unread;
            }
            else
            {
                return;
            }
            bool success = await TtRssInterface.getInterface().updateArticle(current.id, field, UpdateMode.Toggle);
            if (success)
            {
                ArticlesCollection[PivotControl.SelectedIndex].Article = await TtRssInterface.getInterface().getArticle(current.id);
                UpdateLocalizedApplicationBar(ArticlesCollection[PivotControl.SelectedIndex].Article);
            }
            if (remove)
            {
                if (TotalCount == 1)
                {
                    NavigationService.GoBack();
                }
                else
                {
                    TotalCount--;
                    ArticlesCollection.RemoveAt(PivotControl.SelectedIndex);
                }
            }
        }

        private void openExt_Click(object sender, EventArgs e)
        { 
            if (sender == openExtAppBarButton)
            {
                WebBrowserTask wbt = new WebBrowserTask();
                wbt.Uri = new Uri(ArticlesCollection[PivotControl.SelectedIndex].Article.link);
                wbt.Show();
            }
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            Counter.Width = ResolutionHelper.GetWidthForOrientation(Orientation);
            if (Orientation.Equals(PageOrientation.LandscapeLeft) || Orientation.Equals(PageOrientation.LandscapeRight))
            {
                SystemTray.IsVisible = false;
            }
            else
            {
                SystemTray.IsVisible = true;
            }
            PivotItem myPivotItem =
                (PivotItem)(PivotControl.ItemContainerGenerator.ContainerFromItem(PivotControl.Items[PivotControl.SelectedIndex]));

            var wc = FindDescendantByName(myPivotItem, "WebContent") as WebBrowser;
            if (wc != null)
            {
                wc.FontSize = 26;
            }
        }
    }
}