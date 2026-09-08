// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

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
    public class CategoriesViewModelTests
    {
        private TestDbContextFactory factory = null!;
        private CategoriesViewModel viewModel = null!;

        [TestInitialize]
        public void Initialize()
        {
            factory = new TestDbContextFactory();
            viewModel = new CategoriesViewModel(factory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            factory.Dispose();
        }

        [TestMethod]
        public async Task LoadAsync_LoadsOnlyRootCategories()
        {
            using (MaizuruContext context = factory.CreateDbContext())
            {
                Category root = new() { Name = "Root" };
                context.Categories.Add(root);
                await context.SaveChangesAsync();

                Category child = new() { Name = "Child", ParentCategoryId = root.Id };
                context.Categories.Add(child);
                await context.SaveChangesAsync();
            }

            await viewModel.LoadAsync();

            Assert.AreEqual(1, viewModel.Categories.Count);
            Assert.AreEqual("Root", viewModel.Categories[0].Name);
        }

        [TestMethod]
        public async Task AddCommand_WithNullArgument_AddsRootCategory()
        {
            await viewModel.AddCommand.ExecuteAsync(null);

            using MaizuruContext context = factory.CreateDbContext();
            Category created = await context.Categories.SingleAsync();
            Assert.IsNull(created.ParentCategoryId);
            Assert.AreEqual(1, viewModel.Categories.Count);
        }

        [TestMethod]
        public async Task AddCommand_WithExistingCategory_AddsChildCategory()
        {
            Category parent = new() { Name = "Parent" };
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.Categories.Add(parent);
                await context.SaveChangesAsync();
            }

            viewModel.Category = parent;
            await viewModel.AddCommand.ExecuteAsync(null);

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Category child = await context.Categories.SingleAsync(c => c.ParentCategoryId == parent.Id);
                Assert.IsNotNull(child);
            }
        }

        [TestMethod]
        public void InvokeCommand_SendsCategoryInvokedMessage()
        {
            Category cat = new() { Id = 1, Name = "Target" };
            CategoryInvokedMessage? received = null;

            WeakReferenceMessenger.Default.Register<CategoriesViewModelTests, CategoryInvokedMessage>(
                this,
                (r, m) => received = m);

            viewModel.Category = cat;

            Assert.IsTrue(viewModel.InvokeCommand.CanExecute(null));
            viewModel.InvokeCommand.Execute(null);

            Assert.IsNotNull(received);
            Assert.AreEqual("Target", received.Value.Name);
        }

        [TestMethod]
        public void InvokeCommand_WithNull_CannotExecute()
        {
            viewModel.Category = null;
            Assert.IsFalse(viewModel.InvokeCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task RemoveCommand_RemovesCategoryAndReloads()
        {
            Category cat = new() { Name = "ToDelete" };
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.Categories.Add(cat);
                await context.SaveChangesAsync();
            }

            await viewModel.LoadAsync();
            Assert.AreEqual(1, viewModel.Categories.Count);

            viewModel.Category = cat;
            await viewModel.RemoveCommand.ExecuteAsync(null);

            Assert.AreEqual(0, viewModel.Categories.Count);
            using (MaizuruContext context = factory.CreateDbContext())
            {
                Assert.AreEqual(0, await context.Categories.CountAsync());
            }
        }
    }
}
