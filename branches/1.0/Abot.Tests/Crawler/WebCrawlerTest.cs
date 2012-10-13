﻿using Abot.Core;
using Abot.Crawler;
using Abot.Poco;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Abot.Tests.Crawler
{
    [TestFixture]
    public class CrawlerBasicTest
    {
        WebCrawler _unitUnderTest;
        Mock<IPageRequester> _fakeHttpRequester;
        Mock<IHyperLinkParser> _fakeHyperLinkParser;
        Mock<ICrawlDecisionMaker> _mockCrawlDecisionMaker;
        FifoScheduler _dummyScheduler;
        ThreadManager _dummyThreadManager;
        Uri _rootUri;

        [SetUp]
        public void SetUp()
        {
            _fakeHyperLinkParser = new Mock<IHyperLinkParser>();
            _fakeHttpRequester = new Mock<IPageRequester>();
            _mockCrawlDecisionMaker = new Mock<ICrawlDecisionMaker>();

            _dummyScheduler = new FifoScheduler();
            _dummyThreadManager = new ThreadManager(1);

            _unitUnderTest = new WebCrawler(_dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _mockCrawlDecisionMaker.Object);

            _rootUri = new Uri("http://a.com/");
        }

        [Test]
        public void Constructor_Empty()
        {
            Assert.IsNotNull(new WebCrawler());
        }

        [Test]
        public void Crawl_CallsDependencies()
        {
            Uri uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            Uri uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            CrawledPage homePage = new CrawledPage(_rootUri) { RawContent = "content here"};
            CrawledPage page1 = new CrawledPage(uri1);
            CrawledPage page2 = new CrawledPage(uri2);

            List<Uri> links = new List<Uri>{uri1, uri2};

            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri)).Returns(homePage);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri1)).Returns(page1);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri2)).Returns(page2);
            _fakeHyperLinkParser.Setup(f => f.GetLinks(_rootUri, It.IsAny<string>())).Returns(links);
            _mockCrawlDecisionMaker.Setup(f => f.ShouldCrawl(It.IsAny<PageToCrawl>())).Returns(true);
            _mockCrawlDecisionMaker.Setup(f => f.ShouldCrawlLinks(It.IsAny<CrawledPage>())).Returns(true);

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequest(_rootUri), Times.Once());
            _fakeHttpRequester.Verify(f => f.MakeRequest(uri1), Times.Once());
            _fakeHttpRequester.Verify(f => f.MakeRequest(uri2), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(_rootUri, It.IsAny<string>()), Times.Once());
            _mockCrawlDecisionMaker.Verify(f => f.ShouldCrawl(It.IsAny<PageToCrawl>()), Times.Exactly(3));
            _mockCrawlDecisionMaker.Verify(f => f.ShouldCrawlLinks(It.IsAny<CrawledPage>()), Times.Exactly(3));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Crawl_NullUri()
        {
            _unitUnderTest.Crawl(null);
        }


        [Test]
        public void Crawl_PageEventsFire()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<Uri>(), It.IsAny<string>())).Returns(new List<Uri>());
            _mockCrawlDecisionMaker.Setup(f => f.ShouldCrawl(It.IsAny<PageToCrawl>())).Returns(true);
            _mockCrawlDecisionMaker.Setup(f => f.ShouldCrawlLinks(It.IsAny<CrawledPage>())).Returns(true);

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++_pageCrawlStartingCount;
            
            _unitUnderTest.Crawl(_rootUri);
            System.Threading.Thread.Sleep(100);//sleep since the events are async and may not complete before returning

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<Uri>(), It.IsAny<string>()), Times.Once());
            _mockCrawlDecisionMaker.Verify(f => f.ShouldCrawl(It.IsAny<PageToCrawl>()), Times.Once());
            _mockCrawlDecisionMaker.Verify(f => f.ShouldCrawlLinks(It.IsAny<CrawledPage>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
        }

        [Test]
        public void Crawl_EventSubscriberThrowsExceptions_DoesNotCrash()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<Uri>(), It.IsAny<string>())).Returns(new List<Uri>());
            _mockCrawlDecisionMaker.Setup(f => f.ShouldCrawl(It.IsAny<PageToCrawl>())).Returns(true);
            _mockCrawlDecisionMaker.Setup(f => f.ShouldCrawlLinks(It.IsAny<CrawledPage>())).Returns(true);

            FifoScheduler _dummyScheduler = new FifoScheduler();
            ThreadManager _dummyThreadManager = new ThreadManager(1);
            _unitUnderTest = new WebCrawler(_dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _mockCrawlDecisionMaker.Object);

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlStarting += new EventHandler<PageCrawlStartingArgs>(ThrowExceptionWhen_PageCrawlStarting);
            _unitUnderTest.PageCrawlCompleted += new EventHandler<PageCrawlCompletedArgs>(ThrowExceptionWhen_PageCrawlCompleted);
            
            _unitUnderTest.Crawl(_rootUri);
            System.Threading.Thread.Sleep(1000);//sleep since the events are async and may not complete

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<Uri>(), It.IsAny<string>()), Times.Once());
            _mockCrawlDecisionMaker.Verify(f => f.ShouldCrawl(It.IsAny<PageToCrawl>()), Times.Once());
            _mockCrawlDecisionMaker.Verify(f => f.ShouldCrawlLinks(It.IsAny<CrawledPage>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
        }

        [Test]
        public void Crawl_PageCrawlCompletedEvent_IsAsynchronous()
        {
            int elapsedTimeForLongJob = 5000;

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<Uri>(), It.IsAny<string>())).Returns(new List<Uri>());
            _unitUnderTest.PageCrawlCompleted += new EventHandler<PageCrawlCompletedArgs>((sender, args) => System.Threading.Thread.Sleep(elapsedTimeForLongJob));

            Stopwatch timer = Stopwatch.StartNew();
            _unitUnderTest.Crawl(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < elapsedTimeForLongJob);
        }

        [Test]
        public void Crawl_PageCrawlStartingEvent_IsAsynchronous()
        {
            int elapsedTimeForLongJob = 5000;

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<Uri>(), It.IsAny<string>())).Returns(new List<Uri>());
            _unitUnderTest.PageCrawlStarting += new EventHandler<PageCrawlStartingArgs>((sender, args) => System.Threading.Thread.Sleep(elapsedTimeForLongJob));

            Stopwatch timer = Stopwatch.StartNew();
            _unitUnderTest.Crawl(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < elapsedTimeForLongJob);
        }


        [Test]
        public void Crawl_ShouldCrawlReturnsFalse_DoesNotFireEvents()
        {
            _mockCrawlDecisionMaker.Setup(f => f.ShouldCrawl(It.IsAny<PageToCrawl>())).Returns(false);

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++_pageCrawlStartingCount;

            _unitUnderTest.Crawl(_rootUri);
            System.Threading.Thread.Sleep(100);//sleep since the events are async and may not complete before returning

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>()), Times.Never());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<Uri>(), It.IsAny<string>()), Times.Never());
            _mockCrawlDecisionMaker.Verify(f => f.ShouldCrawl(It.IsAny<PageToCrawl>()), Times.Once());
            _mockCrawlDecisionMaker.Verify(f => f.ShouldCrawlLinks(It.IsAny<CrawledPage>()), Times.Never());

            Assert.AreEqual(0, _pageCrawlStartingCount);
            Assert.AreEqual(0, _pageCrawlCompletedCount);
        }


        private void ThrowExceptionWhen_PageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            throw new Exception("no!!!");
        }

        private void ThrowExceptionWhen_PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            throw new Exception("Oh No!");
        }
    }
}