// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Maizuru.Contexts;
using Maizuru.Messages;
using Maizuru.Models;
using Maizuru.Test.Fakes;
using Maizuru.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maizuru.Test.ViewModels
{
    [TestClass]
    public class ItemsViewModelTests
    {
        private TestDbContextFactory factory = null!;
        private FakeEmbeddingGenerator embeddingGenerator = null!;
        private ItemsViewModel viewModel = null!;
        private Category category1 = null!;
        private Category category2 = null!;
        private PaymentMethod paymentMethod1 = null!;
        private PaymentMethod paymentMethod2 = null!;

        [TestInitialize]
        public void Initialize()
        {
            factory = new TestDbContextFactory();
            embeddingGenerator = new FakeEmbeddingGenerator();
            viewModel = new ItemsViewModel(embeddingGenerator, factory);

            using MaizuruContext context = factory.CreateDbContext();
            category1 = new Category { Name = "Food" };
            category2 = new Category { Name = "Utilities" };
            paymentMethod1 = new PaymentMethod { Name = "Cash" };
            paymentMethod2 = new PaymentMethod { Name = "Card" };
            context.Categories.AddRange(category1, category2);
            context.PaymentMethods.AddRange(paymentMethod1, paymentMethod2);
            context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            factory.Dispose();
        }

        [TestMethod]
        public async Task LoadAsync_OrdersItemsByDateTimeDescendingThenById()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.Items.Add(new Item
                {
                    Name = "Old",
                    DateTimeOffset = now.AddDays(-2),
                    CategoryId = category1.Id,
                    PaymentMethodId = paymentMethod1.Id
                });
                context.Items.Add(new Item
                {
                    Name = "Newer1",
                    DateTimeOffset = now,
                    CategoryId = category1.Id,
                    PaymentMethodId = paymentMethod1.Id
                });
                context.Items.Add(new Item
                {
                    Name = "Newer2",
                    DateTimeOffset = now,
                    CategoryId = category1.Id,
                    PaymentMethodId = paymentMethod1.Id
                });
                await context.SaveChangesAsync();
            }

            await viewModel.LoadAsync();

            Assert.AreEqual(3, viewModel.Items.Count);
            Assert.AreEqual("Newer1", viewModel.Items[0].Name);
            Assert.AreEqual("Newer2", viewModel.Items[1].Name);
            Assert.AreEqual("Old", viewModel.Items[2].Name);
        }

        [TestMethod]
        public async Task AddCommand_AssignsFirstCategoryAndPaymentMethod()
        {
            await viewModel.LoadAsync();
            await viewModel.AddCommand.ExecuteAsync(null);

            Assert.AreEqual(1, viewModel.Items.Count);
            Item added = viewModel.Items[0];
            Assert.AreEqual(category1.Id, added.CategoryId);
            Assert.AreEqual(paymentMethod1.Id, added.PaymentMethodId);
        }

        [TestMethod]
        public async Task RemoveCommand_RemovesItemAndReloads()
        {
            Item item = new()
            {
                Name = "ToDelete",
                CategoryId = category1.Id,
                PaymentMethodId = paymentMethod1.Id
            };
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.Items.Add(item);
                await context.SaveChangesAsync();
            }

            await viewModel.LoadAsync();
            Assert.AreEqual(1, viewModel.Items.Count);

            viewModel.Item = viewModel.Items[0];
            await viewModel.RemoveCommand.ExecuteAsync(null);

            Assert.AreEqual(0, viewModel.Items.Count);
        }

        [TestMethod]
        public void InvokeCommand_SendsItemInvokedMessage()
        {
            Item item = new() { Id = 7, Name = "Coffee" };
            ItemInvokedMessage? received = null;

            WeakReferenceMessenger.Default.Register<ItemsViewModelTests, ItemInvokedMessage>(
                this,
                (r, m) => received = m);

            viewModel.Item = item;

            Assert.IsTrue(viewModel.InvokeCommand.CanExecute(null));
            viewModel.InvokeCommand.Execute(null);

            Assert.IsNotNull(received);
            Assert.AreEqual("Coffee", received.Value.Name);
        }

        [TestMethod]
        public void InvokeCommand_WithNull_CannotExecute()
        {
            viewModel.Item = null;
            Assert.IsFalse(viewModel.InvokeCommand.CanExecute(null));
        }

        [TestMethod]
        public void CanFilter_EvaluatesCorrectly()
        {
            Assert.IsFalse(viewModel.FilterCommand.CanExecute(""));
            Assert.IsFalse(viewModel.FilterCommand.CanExecute("   "));

            Assert.IsTrue(viewModel.FilterCommand.CanExecute("lunch"));

            viewModel.Category = category1;
            Assert.IsTrue(viewModel.FilterCommand.CanExecute(""));

            viewModel.Category = null;
            viewModel.PaymentMethod = paymentMethod1;
            Assert.IsTrue(viewModel.FilterCommand.CanExecute(""));
        }

        [TestMethod]
        public async Task FilterCommand_ByCategory_FiltersMatchingItems()
        {
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.Items.Add(new Item { Name = "Item1", CategoryId = category1.Id, PaymentMethodId = paymentMethod1.Id });
                context.Items.Add(new Item { Name = "Item2", CategoryId = category2.Id, PaymentMethodId = paymentMethod1.Id });
                await context.SaveChangesAsync();
            }

            viewModel.Category = category2;
            await viewModel.FilterCommand.ExecuteAsync("");

            Assert.AreEqual(1, viewModel.Items.Count);
            Assert.AreEqual("Item2", viewModel.Items[0].Name);
        }

        [TestMethod]
        public async Task FilterCommand_ByPaymentMethod_FiltersMatchingItems()
        {
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.Items.Add(new Item { Name = "Item1", CategoryId = category1.Id, PaymentMethodId = paymentMethod1.Id });
                context.Items.Add(new Item { Name = "Item2", CategoryId = category1.Id, PaymentMethodId = paymentMethod2.Id });
                await context.SaveChangesAsync();
            }

            viewModel.PaymentMethod = paymentMethod2;
            await viewModel.FilterCommand.ExecuteAsync("");

            Assert.AreEqual(1, viewModel.Items.Count);
            Assert.AreEqual("Item2", viewModel.Items[0].Name);
        }

        [TestMethod]
        public async Task FilterCommand_SemanticVectorSearch_FiltersAboveThreshold()
        {
            // Vector dot product threshold is MATCH_BORDER_LINE = 0.75f
            float[] matchingVector = new float[Constants.DIMENSIONS];
            matchingVector[0] = 0.9f; // dot product with queryVector = 0.9 * 1.0 = 0.9 > 0.75

            float[] nonMatchingVector = new float[Constants.DIMENSIONS];
            nonMatchingVector[0] = 0.4f; // dot product with queryVector = 0.4 * 1.0 = 0.4 < 0.75

            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.Items.Add(new Item
                {
                    Name = "Relevant Item",
                    CategoryId = category1.Id,
                    PaymentMethodId = paymentMethod1.Id,
                    Vector = matchingVector
                });
                context.Items.Add(new Item
                {
                    Name = "Irrelevant Item",
                    CategoryId = category1.Id,
                    PaymentMethodId = paymentMethod1.Id,
                    Vector = nonMatchingVector
                });
                await context.SaveChangesAsync();
            }

            // Mock query embedding: returns vector with 1.0 at index 0
            float[] queryVector = new float[Constants.DIMENSIONS];
            queryVector[0] = 1.0f;
            embeddingGenerator.GeneratorFunc = (prompt, options) => queryVector;

            await viewModel.FilterCommand.ExecuteAsync("search phrase");

            Assert.AreEqual(1, viewModel.Items.Count);
            Assert.AreEqual("Relevant Item", viewModel.Items[0].Name);
            Assert.AreEqual("task: search result | query: search phrase", embeddingGenerator.Invocations[0].Prompt);
        }
    }
}
