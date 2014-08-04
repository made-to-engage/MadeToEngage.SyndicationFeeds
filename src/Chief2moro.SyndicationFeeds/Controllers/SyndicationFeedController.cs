﻿using System.Linq;
using System.ServiceModel.Syndication;
using System.Web.Mvc;
using Chief2moro.SyndicationFeeds.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc;

namespace Chief2moro.SyndicationFeeds.Controllers
{
    public class SyndicationFeedController : PageController<SyndicationFeedPageType>
    {
        private readonly IContentLoader _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        private readonly IFeedContentResolver _feedContentResolver = ServiceLocator.Current.GetInstance<IFeedContentResolver>();

        public ActionResult Index(SyndicationFeedPageType currentPage)
        {
            var syndicationFactory = new SyndicationFactory(currentPage);
            
            var feed = new SyndicationFeed
            {
                Items = syndicationFactory.GetSyndicationItems(),
                Id = currentPage.ContentGuid.ToString(),
                Title = new TextSyndicationContent(currentPage.PageName),
                Description = new TextSyndicationContent(currentPage.Description),
            };

            if (feed.Items.Any())
                feed.LastUpdatedTime = feed.Items.Max(m => m.LastUpdatedTime);

            if (currentPage.FeedFormat == FeedFormat.Atom)
                return new AtomActionResult(feed);
           
            return new RssActionResult(feed);
        }

        public ActionResult Item(SyndicationFeedPageType currentPage, int? contentId)
        {
            if (!contentId.HasValue)
                return HttpNotFound("No content Id specified");

            var contentReference = ContentReference.Parse(contentId.Value.ToString());

            var referencedContent = _feedContentResolver.GetContentReferences(currentPage);
            if (!referencedContent.Contains(contentReference))
                return HttpNotFound("Content Id not exposed in this feed");
            
            var contentArea = new ContentArea();
            var item = new ContentAreaItem {ContentLink = contentReference};
            contentArea.Items.Add(item);

            var contentItem = _contentLoader.Get<IContent>(contentReference);

            var model = new ContentHolderModel { Tag = currentPage.BlockRenderingTag, ContentArea = contentArea, Content = contentItem};
            return View("~/modules/Chief2moro.SyndicationFeeds/Views/Item.cshtml", model);
        }
    }
}