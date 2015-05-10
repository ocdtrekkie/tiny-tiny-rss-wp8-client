using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyTinyRSS;
using TinyTinyRSS.Classes;
using TinyTinyRSS.Interface;
using TinyTinyRSS.Interface.Classes;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TinyTinyRSS
{
    public abstract class AbstractArticlePage : Page
    {
        protected int feedId;
        protected Collection<WrappedArticle> ArticlesCollection;
        protected ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        protected bool _showUnreadOnly, _moreArticles, _moreArticlesLoading;
        protected int _sortOrder;
        protected bool initialized;

        protected abstract void updateCount(bool p);
        protected abstract int getSelectedIdx();
        protected abstract void SetProgressBar(bool on, bool showText);

        protected void SetProgressBar(bool on)
        {
            SetProgressBar(on, false);
        }

        protected void ShareAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        protected void RegisterForShare()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>(this.ShareLinkHandler);
        }

        protected void ShareLinkHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            Headline head = ArticlesCollection[getSelectedIdx()].Headline;
            DataRequest request = e.Request;
            request.Data.Properties.Description = "Shared by tt-RSS Reader for Windows Phone.";
            request.Data.Properties.Title = head.title;
            request.Data.SetWebLink(new Uri(head.link));
        }

        protected async void openExt_Click(object sender, RoutedEventArgs e)
        {
            if (getSelectedIdx() < 0)
            {
                return;
            }
            WrappedArticle article = ArticlesCollection[getSelectedIdx()];
            if (article.Article != null)
            {
                var uri = new Uri(article.Article.link);
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
            else
            {
                Article art = await article.getContent();
                var uri = new Uri(art.link);
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }

        /// <summary>
        /// Get's the headlines of the shown feed from tt-rss. 
        /// Depending on the settings, may only unread articles are loaded.
        /// </summary>
        /// <returns>true, cause void Tasks don't work.</returns>
        protected async Task<bool> LoadHeadlines()
        {
            try
            {
                SetProgressBar(true);
                bool unReadOnly = !_IsSpecial() && _showUnreadOnly;
                ArticlesCollection.Clear();
                List<Headline> headlines = await TtRssInterface.getInterface().getHeadlines(feedId, unReadOnly, 0, _sortOrder);
                if (headlines.Count == 0)
                {
                    _moreArticles = false;
                    MessageDialog msgbox = new MessageDialog(loader.GetString("NoArticlesMessage"));
                    await msgbox.ShowAsync();
                    Frame rootFrame = Window.Current.Content as Frame;
                    if (rootFrame.CanGoBack)
                    {
                        rootFrame.GoBack();
                    }
                    else
                    {
                        Frame.Navigate(typeof(MainPage));
                    }
                }
                else
                {
                    foreach (Headline h in headlines)
                    {
                        ArticlesCollection.Add(new WrappedArticle(h));
                    }
                    updateCount(false);
                }
            }
            catch (TtRssException ex)
            {
                checkException(ex);
            }
            finally
            {
                SetProgressBar(false);
            }
            return true;
        }

        protected async void checkException(TtRssException ex)
        {
            if (ex.Message.Equals(TtRssInterface.NONETWORKERROR))
            {
                MessageDialog msgbox = new MessageDialog(loader.GetString("NoConnection"));
                await msgbox.ShowAsync();
            }
        }

        protected List<string> getSortOptions()
        {
            List<string> result = new List<string>();
            switch (_sortOrder)
            {
                case 1:
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("ArticlesSortDefault"));
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("ArticlesSortOld"));
                    break;
                case 2:
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("ArticlesSortDefault"));
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("ArticlesSortNew"));
                    break;
                default:
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("ArticlesSortNew"));
                    result.Add(loader.GetString("AppBarSortLabel") + loader.GetString("ArticlesSortOld"));
                    break;
            }
            return result;
        }


        /// <summary>
        /// Check if the shown feed is a special on (archived, unread, etc.)
        /// </summary>
        /// <returns>True if feedId is in between -4 and 1</returns>
        protected bool _IsSpecial()
        {
            return feedId > -4 && feedId < 1;
        }

        /// <summary>
        /// When a pivot item is loaded check if you need to load more articles, cause of lazy loading.
        /// </summary>
        protected async Task LoadMoreHeadlines()
        {
            if (_moreArticles && !_moreArticlesLoading)
            {
                try
                {
                    _moreArticlesLoading = true;
                    SetProgressBar(true, true);
                    bool unReadOnly = !_IsSpecial() && _showUnreadOnly;
                    
                    List<Headline> headlines = await TtRssInterface.getInterface().getHeadlines(feedId, unReadOnly, 0, _sortOrder);

                    if (headlines.Count <= 0)
                    {
                        _moreArticles = false;
                    }
                    else
                    {
                        bool newItems = false;
                        foreach (Headline h in headlines)
                        {
                            bool contains = false;
                            foreach (WrappedArticle a in ArticlesCollection)
                            {
                                if (a.Headline.id == h.id)
                                {
                                    contains = true;
                                    break;
                                }
                            }
                            if (!contains)
                            {
                                ArticlesCollection.Add(new WrappedArticle(h));
                                newItems = true;
                            }
                        }
                        _moreArticles = newItems;
                        updateCount(false);
                    }
                }
                catch (TtRssException ex)
                {
                    checkException(ex);
                }
                finally
                {
                    _moreArticlesLoading = false;
                    SetProgressBar(false);
                }
            }
        }
    }
}
