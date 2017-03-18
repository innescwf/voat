﻿#region LICENSE

/*

    This source file is subject to version 3 of the GPL license,
    that is bundled with this package in the file LICENSE, and is
    available online at http://www.gnu.org/licenses/gpl-3.0.txt;
    you may not use this file except in compliance with the License.

    Software distributed under the License is distributed on an
    "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express
    or implied. See the License for the specific language governing
    rights and limitations under the License.

    All portions of the code written by Voat, Inc. are Copyright(c) Voat, Inc.

    All Rights Reserved.

*/

#endregion LICENSE

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voat.Common;
using Voat.Data;
using Voat.Data.Models;
using Voat.Domain.Command;
using Voat.Domain.Models;

namespace Voat.Tests.Repository
{
    [TestClass]
    public class RepositoryTests : BaseUnitTest
    {
        [TestMethod]
        [TestCategory("Repository"), TestCategory("Repository.Block"), TestCategory("Repository.Block.Subverse")]
        public async Task Block_Subverse()
        {
            using (var db = new Voat.Data.Repository())
            {
                string name = "whatever";

                TestHelper.SetPrincipal("TestUser01");
                await db.Block(DomainType.Subverse, name);

                var blocks = await db.GetBlockedSubverses("TestUser01");
                Assert.IsNotNull(blocks);
                Assert.IsTrue(blocks.Any(x => x.Name == name && x.Type == DomainType.Subverse));

                await db.Unblock(DomainType.Subverse, name);
                blocks = await db.GetBlockedSubverses("TestUser01");
                Assert.IsNotNull(blocks);
                Assert.IsFalse(blocks.Any(x => x.Name == name && x.Type == DomainType.Subverse));
            }
        }

        [TestMethod]
        [TestCategory("Repository"), TestCategory("Repository.Block"), TestCategory("Repository.Block.Subverse")]
        [ExpectedException(typeof(VoatSecurityException))]
        public async Task Block_Subverse_NoAuthentication()
        {
            TestHelper.SetPrincipal(null);
            using (var db = new Voat.Data.Repository())
            {
                await db.Block(DomainType.Subverse, "test");
            }
        }

        [TestMethod]
        [TestCategory("Repository"), TestCategory("Repository.Block"), TestCategory("Repository.Block.Subverse")]
        [ExpectedException(typeof(VoatNotFoundException))]
        public async Task Block_Subverse_SubverseDoesNotExist()
        {
            using (var db = new Voat.Data.Repository())
            {
                TestHelper.SetPrincipal("TestUser01");
                await db.Block(DomainType.Subverse, "happyhappyjoyjoy");
            }
        }

        [TestMethod]
        [TestCategory("Repository"), TestCategory("Repository.Block"), TestCategory("Repository.Block.Subverse")]
        public async Task Block_Subverse_Toggle()
        {
            using (var db = new Voat.Data.Repository())
            {
                string name = "whatever";
                string userName = "TestUser02";

                TestHelper.SetPrincipal(userName);

                await db.Block(DomainType.Subverse, name, SubscriptionAction.Toggle);

                var blocks = await db.GetBlockedSubverses(userName);
                Assert.IsNotNull(blocks);
                Assert.IsTrue(blocks.Any(x => x.Name == name && x.Type == DomainType.Subverse));

                await db.Block(DomainType.Subverse, name, SubscriptionAction.Toggle);

                blocks = await db.GetBlockedSubverses(userName);
                Assert.IsNotNull(blocks);
                Assert.IsFalse(blocks.Any(x => x.Name == name && x.Type == DomainType.Subverse));
            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        //[ExpectedException(typeof(VoatValidationException))]
        public async Task PostSubmission_InvalidSubveseFails()
        {
            using (var db = new Voat.Data.Repository())
            {
                TestHelper.SetPrincipal("TestUser01");

                var response = await db.PostSubmission(new UserSubmission()
                {
                    Subverse = "** Invalid Subverse * *",
                    Content = "Test - " + Guid.NewGuid().ToString(),
                    Title = "My title - PostSubmission_InvalidSubveseFails",
                    Url = "http://www.yahoo.com"
                });
                Assert.IsFalse(response.Success);
                Assert.AreEqual(response.Message, "Subverse does not exist");
            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        public void GetAnonymousComments()
        {
            using (var db = new Voat.Data.Repository())
            {
                var comments = db.GetCommentTree(2, null, null);
                foreach (var c in comments)
                {
                    Assert.IsTrue(c.IsAnonymized);
                }
            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        public void GetAnonymousSubmission()
        {
            using (var db = new Voat.Data.Repository())
            {
                var anon_sub = db.GetSubmission(2);
                Assert.IsTrue(anon_sub.UserName == anon_sub.ID.ToString());
            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        public async Task GetSubmission()
        {
            using (var db = new Voat.Data.Repository())
            {
                var s = await db.GetSubmissionsDapper(new DomainReference(DomainType.Subverse, "unit"), new SearchOptions()).ConfigureAwait(false);
                Assert.IsTrue(s.Any());
            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        [TestCategory("Anon")]
        public async Task GetSubmissionsFilterAnonymous()
        {
            using (var db = new Voat.Data.Repository())
            {
                var anon_sub = await db.GetSubmissionsDapper(new DomainReference(DomainType.Subverse, "anon"), SearchOptions.Default).ConfigureAwait(false);
                var first = anon_sub.OrderBy(x => x.CreationDate).First();
                Assert.IsNotNull(first, "no anon submissions found");
                Assert.AreEqual("First Anon Post", first.Title);
                Assert.AreEqual(first.UserName, first.ID.ToString());
            }
        }

        [TestMethod]
        [TestCategory("Repository"), TestCategory("User"), TestCategory("User.Preferences")]
        public async Task GetUserPrefernces()
        {
            using (var db = new Voat.Data.Repository())
            {
                var p = await db.GetUserPreferences("unit");
                Assert.IsTrue(p != null);
                Assert.IsTrue(p.UserName == "unit");
                Assert.IsTrue(p.Bio == "User unit's short bio");
                Assert.IsTrue(!p.DisableCSS);
            }
        }

        [TestMethod]
        [TestCategory("Repository"), TestCategory("User"), TestCategory("User.Preferences")]
        public async Task GetUserPrefernces_UserNotExists()
        {
            using (var db = new Voat.Data.Repository())
            {
                var p = await db.GetUserPreferences("asrtastarstarstarstart343");
                Assert.AreNotEqual(null, p);
            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        public async Task PostSubmission()
        {
            using (var db = new Voat.Data.Repository())
            {
                TestHelper.SetPrincipal("TestUser01");

                var m = await db.PostSubmission(new UserSubmission()
                {
                    Subverse = "unit",
                    Url = "http://www.LearnToGolfLikeJordanSpiethOrYourMoneyBack.com",
                    Content = "Learn to putt first. It's the most important part of golf.",
                    Title = "Golf is really three games in one: Putting, Full Swing, and Partial Swing"
                });

                Assert.IsNotNull(m, "CommandResponse is null");
                Assert.IsNotNull(m.Response, "Response payload is null");
                Assert.AreNotEqual(0, m.Response.ID);
            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        public void SubverseRetrieval()
        {
            using (var db = new Voat.Data.Repository())
            {
                var info = db.GetSubverseInfo("unit");
                Assert.IsTrue(info.Title == "v/unit");
            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        public async Task SaveComment()
        {
            using (var db = new Voat.Data.Repository())
            {
                var result = await db.Save(ContentType.Comment, 1);
                Assert.AreEqual(Status.Success, result.Status);
                Assert.AreEqual(true, result.Response.Value);
            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        public async Task SaveComment_Force()
        {
            using (var db = new Voat.Data.Repository())
            {
                var result =  await db.Save(ContentType.Comment, 3, true);
                Assert.AreEqual(Status.Success, result.Status);
                Assert.AreEqual(true, result.Response.Value);

                //Should only save, never toggle because forceAction == true
                result = await db.Save(ContentType.Comment, 3, true);
                Assert.AreEqual(Status.Success, result.Status);
                Assert.AreEqual(true, result.Response.Value);

            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        public async Task SaveComment_ForceUnSave()
        {
            using (var db = new Voat.Data.Repository())
            {
                var result = await db.Save(ContentType.Comment, 4, false);
                Assert.AreEqual(Status.Success, result.Status);
                Assert.AreEqual(false, result.Response.Value);

                //Should only save, never toggle because forceAction == true
                result = await db.Save(ContentType.Comment, 3, false);
                Assert.AreEqual(Status.Success, result.Status);
                Assert.AreEqual(false, result.Response.Value);
            }
        }

        [TestMethod]
        [TestCategory("Repository")]
        public async Task SaveComment_Toggle()
        {
            using (var db = new Voat.Data.Repository())
            {
                var result = await db.Save(ContentType.Comment, 2);
                Assert.AreEqual(Status.Success, result.Status);
                Assert.AreEqual(true, result.Response.Value);

                result = await db.Save(ContentType.Comment, 2);
                Assert.AreEqual(Status.Success, result.Status);
                Assert.AreEqual(false, result.Response.Value);
            } 
        }



        [TestMethod]
        [TestCategory("Repository"), TestCategory("Repository.Submission")]
        [TestCategory("Ban"), TestCategory("Ban.Domain")]
        public async Task PostSubmission_BannedDomain()
        {
            using (var db = new Voat.Data.Repository())
            {
                TestHelper.SetPrincipal("TestUser10");

                var result = await db.PostSubmission(new UserSubmission() { Subverse = "unit", Title = "Can I get a banned domain past super secure code?", Content = "Check out my new post: http://www.fleddit.com/r/something/hen9s87r9/How-I-Made-a-million-virtual-cat-pics" });
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Success, "Submitting content with banned domain did not get rejected");
                Assert.AreEqual(Status.Denied, result.Status, "Expecting a denied status");
            }
        }

        [TestMethod]
        [TestCategory("Repository"), TestCategory("Repository.Comment")]
        [TestCategory("Ban"), TestCategory("Ban.Domain")]
        public async Task PostComment_BannedDomain()
        {
            using (var db = new Voat.Data.Repository())
            {
                TestHelper.SetPrincipal("TestUser10");

                var result = await db.PostComment(1, null, "Check out my new post: http://www.fleddit.com/r/something/hen9s87r9/How-I-Made-a-million-virtual-cat-pics");
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Success, "Submitting content with banned domain did not get rejected");
                Assert.AreEqual(Status.Denied, result.Status, "Expecting a denied status");
            }
        }

        [TestMethod]
        [TestCategory("Repository"), TestCategory("Repository.Submission")]
        public async Task PostSubmission_AuthorizedOnly_Allow()
        {
            using (var db = new Voat.Data.Repository())
            {
                TestHelper.SetPrincipal("unit");

                var result = await db.PostSubmission(new UserSubmission() { Subverse = "AuthorizedOnly", Title = "Ha ha, you can't stop me", Content = "Cookies for you my friend" });
                Assert.IsNotNull(result, "Result was null");
                Assert.IsTrue(result.Success, "Submitting to authorized only subverse was not allowed by admin");
                Assert.AreEqual(Status.Success, result.Status, "Expecting a success status");
            }
        }

        [TestMethod]
        [TestCategory("Repository"), TestCategory("Repository.Submission")]
        public async Task PostSubmission_AuthorizedOnly_Denied()
        {
            using (var db = new Voat.Data.Repository())
            {
                TestHelper.SetPrincipal("TestUser11");

                var result = await db.PostSubmission( new UserSubmission() { Subverse= "AuthorizedOnly", Title = "Ha ha, you can't stop me", Content = "Cookies for you my friend" });
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Success, "Submitting to authorized only subverse was allowed by non admin");
                Assert.AreEqual(Status.Denied, result.Status, "Expecting a denied status");
            }
        }



        [TestMethod]
        [TestCategory("Repository"), TestCategory("Repository.Submission")]
        public void Get_SubversesUser_Moderates()
        {

            using (var db = new Voat.Data.Repository())
            {
                var result = db.GetSubversesUserModerates("unit");
                Assert.IsNotNull(result, "Result was null");
                Assert.IsTrue(result.Any(x => x.Subverse == "AuthorizedOnly"), "Result expected to see subverse AuthorizedOnly for user unit");
            }
        }

        [TestMethod]
        [TestCategory("Repository"), TestCategory("Ban"), TestCategory("Ban.Domain")]
        public void BannedDomainTest()
        {
            var domain = "paydayloansforeverybody.com";
            var reason = "Total Rip off!";
            var createdBy = "AntiSpamcist";
            var createdDate = DateTime.UtcNow.AddDays(-10);

            using (var db = new voatEntities())
            {
                db.BannedDomains.Add(new BannedDomain()
                {
                    Domain = domain,
                    Reason = reason,
                    CreatedBy = createdBy,
                    CreationDate = createdDate
                });
                db.SaveChanges();
            }
            using (var repo = new Voat.Data.Repository())
            {
                var result = repo.BannedDomains(new string[] { "yahoo.com", "google.com", domain, domain.ToUpper(), "testuri.org" });
                Assert.IsNotNull(result, "Result was null");
                Assert.IsTrue(result.Any(), "Result expected");
                Assert.AreEqual(1, result.Count(), "Count off");
                var bd = result.First();
                Assert.AreEqual(domain, bd.Domain);
                Assert.AreEqual(reason, bd.Reason);
                Assert.AreEqual(createdBy, bd.CreatedBy);
                Assert.AreEqual(createdDate.ToString(), bd.CreationDate.ToString());

                result = repo.BannedDomains(new string[] { "subdomain." + domain });
                Assert.IsNotNull(result, "subdomain failure");
                Assert.IsTrue(result.Any(), "Subdomain Result expected");
            }
        }

        [TestMethod]
        [TestCategory("Repository"), TestCategory("Ban"), TestCategory("Ban.Domain")]
        public void BannedDomainTest_Root()
        {
            var domain = "ru";
            var reason = "Russians!";
            var createdBy = "AntiSpamcist";
            var createdDate = DateTime.UtcNow.AddDays(-10);

            using (var db = new voatEntities())
            {
                db.BannedDomains.Add(new BannedDomain()
                {
                    Domain = domain,
                    Reason = reason,
                    CreatedBy = createdBy,
                    CreationDate = createdDate
                });
                db.SaveChanges();
            }
            using (var repo = new Voat.Data.Repository())
            {
                IEnumerable<BannedDomain> result;

                //Test invalid
                result = repo.BannedDomains(new string[] { "invalid" }, 1);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { "invalid" }, 100);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { "invalid" }, null);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { null }, null);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { null }, 1);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { null }, -1001);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { "arst" }, -1001);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");


                //Test ru
                result = repo.BannedDomains(new string[] { "sub.domain.ru" }, 1);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsTrue(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { "deep.sub.domain.ru" }, 1);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsTrue(result.Any(), "Result expected");

                //Test domain.sx
                result = repo.BannedDomains(new string[] { "sub.domain.ru" }, 2);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { "sub.domain.ru" }, 3);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { "sub.domain.ru" }, 3);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { "sub.domain.ru" }, null);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsFalse(result.Any(), "Result expected");

                result = repo.BannedDomains(new string[] { "sub.domain.ru" }, -1000);
                Assert.IsNotNull(result, "Result was null");
                Assert.IsTrue(result.Any(), "Result expected");

                //make sure defaults don't change
                result = repo.BannedDomains(new string[] { "sub.domain.ru" });
                Assert.IsNotNull(result, "Result was null");
                Assert.IsTrue(result.Any(), "Result expected");

                
            }
        }
        [TestMethod]
        [TestCategory("Repository"), TestCategory("Repository.LogVisit")]
        public async Task EnsureLogVisitWorks()
        {
            using (var repo = new Voat.Data.Repository())
            {
                await repo.LogVisit(1, "127.0.0.1");    
            }
        }
    }
}