// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;
using System.Threading.Tasks;
using Maizuru.Contexts;
using Maizuru.Models;
using Maizuru.Test.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maizuru.Test.Contexts
{
    [TestClass]
    public class MaizuruContextTests
    {
        private TestDbContextFactory factory = null!;

        [TestInitialize]
        public void Initialize()
        {
            factory = new TestDbContextFactory();
        }

        [TestCleanup]
        public void Cleanup()
        {
            factory.Dispose();
        }

        [TestMethod]
        public async Task DbSets_AreAccessible()
        {
            using MaizuruContext context = factory.CreateDbContext();

            Assert.AreEqual(0, await context.Categories.CountAsync());
            Assert.AreEqual(0, await context.Items.CountAsync());
            Assert.AreEqual(0, await context.PaymentMethods.CountAsync());
        }

        [TestMethod]
        public async Task CategoryHierarchy_DeleteParent_SetsChildParentIdToNull()
        {
            int parentId;
            int childId;

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Category parent = new() { Name = "Parent" };
                Category child = new() { Name = "Child", ParentCategory = parent };

                context.Categories.Add(parent);
                context.Categories.Add(child);
                await context.SaveChangesAsync();

                parentId = parent.Id;
                childId = child.Id;
            }

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Category parent = await context.Categories.FindAsync(parentId) ?? throw new InvalidOperationException();
                context.Categories.Remove(parent);
                await context.SaveChangesAsync();
            }

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Category child = await context.Categories.FindAsync(childId) ?? throw new InvalidOperationException();
                Assert.IsNull(child.ParentCategoryId);
            }
        }

        [TestMethod]
        public async Task Category_LoadsChildCategoriesWithInclude()
        {
            int parentId;

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Category parent = new() { Name = "Parent" };
                Category child = new() { Name = "Child", ParentCategory = parent };

                context.Categories.AddRange(parent, child);
                await context.SaveChangesAsync();
                parentId = parent.Id;
            }

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Category loadedParent = await context.Categories
                    .Include(c => c.Categories)
                    .FirstAsync(c => c.Id == parentId);
                Assert.AreEqual(1, loadedParent.Categories.Count);
                Assert.AreEqual("Child", loadedParent.Categories.First().Name);
            }
        }

        [TestMethod]
        public async Task ItemVector_PersistsAndRestoresCorrectly()
        {
            int itemId;
            float[] sampleVector = new float[Constants.DIMENSIONS];
            sampleVector[0] = 0.42f;
            sampleVector[10] = -0.99f;
            sampleVector[Constants.DIMENSIONS - 1] = 1.23f;

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Category category = new() { Name = "Cat1" };
                PaymentMethod pm = new() { Name = "Cash" };
                context.Categories.Add(category);
                context.PaymentMethods.Add(pm);
                await context.SaveChangesAsync();

                Item item = new()
                {
                    Name = "Sample Item",
                    CategoryId = category.Id,
                    PaymentMethodId = pm.Id,
                    Vector = sampleVector
                };
                context.Items.Add(item);
                await context.SaveChangesAsync();
                itemId = item.Id;
            }

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Item restored = await context.Items.FindAsync(itemId) ?? throw new InvalidOperationException();
                Assert.IsNotNull(restored.Vector);
                Assert.AreEqual(Constants.DIMENSIONS, restored.Vector.Length);
                Assert.AreEqual(0.42f, restored.Vector[0], 1e-5f);
                Assert.AreEqual(-0.99f, restored.Vector[10], 1e-5f);
                Assert.AreEqual(1.23f, restored.Vector[Constants.DIMENSIONS - 1], 1e-5f);
            }
        }
    }
}
