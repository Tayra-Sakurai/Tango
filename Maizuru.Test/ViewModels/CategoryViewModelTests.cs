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
    public class CategoryViewModelTests
    {
        private TestDbContextFactory factory = null!;
        private CategoryViewModel viewModel = null!;

        [TestInitialize]
        public void Initialize()
        {
            factory = new TestDbContextFactory();
            viewModel = new CategoryViewModel(factory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            factory.Dispose();
        }

        [TestMethod]
        public void Name_Empty_FailsValidationAndDisablesSave()
        {
            viewModel.Name = "Initial";
            viewModel.Name = "";

            Assert.IsTrue(viewModel.HasErrors);
            Assert.IsFalse(viewModel.SaveCommand.CanExecute(null));
        }

        [TestMethod]
        public void Name_Provided_PassesValidationAndEnablesSave()
        {
            viewModel.Name = "Food";

            Assert.IsFalse(viewModel.HasErrors);
            Assert.IsTrue(viewModel.SaveCommand.CanExecute(null));
        }

        [TestMethod]
        public void ParentCategory_SetToSelf_ResetsToNull()
        {
            Category self = new() { Id = 42, Name = "SelfCategory" };
            viewModel.Categories.Add(self);
            viewModel.LoadExisitngValue(self);

            viewModel.ParentCategory = self;

            Assert.IsNull(viewModel.ParentCategory);
        }

        [TestMethod]
        public async Task LoadAsync_PopulatesCategoriesWithNullLeadingOption()
        {
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.Categories.Add(new Category { Name = "Root1" });
                context.Categories.Add(new Category { Name = "Root2" });
                Category child = new() { Name = "Child" };
                context.Categories.Add(child);
                await context.SaveChangesAsync();

                // make it a child
                child.ParentCategoryId = 1;
                await context.SaveChangesAsync();
            }

            await viewModel.LoadAsync();

            Assert.IsTrue(viewModel.Categories.Count >= 3);
            Assert.IsNull(viewModel.Categories[0]);
            Assert.IsTrue(viewModel.Categories.Skip(1).All(c => c!.ParentCategoryId == null));
        }

        [TestMethod]
        public async Task SaveCommand_PersistsCategoryToDb()
        {
            viewModel.Name = "Groceries";
            viewModel.Description = "Daily groceries";

            await viewModel.SaveCommand.ExecuteAsync(null);

            using MaizuruContext context = factory.CreateDbContext();
            Category saved = await context.Categories.FirstAsync(c => c.Name == "Groceries");
            Assert.AreEqual("Daily groceries", saved.Description);
        }

        [TestMethod]
        public async Task RemoveCommand_RemovesFromDbAndSendsMessage()
        {
            Category cat = new() { Name = "ToDelete" };
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.Categories.Add(cat);
                await context.SaveChangesAsync();
            }

            viewModel.LoadExisitngValue(cat);

            CategoryRemovedMessage? receivedMessage = null;
            WeakReferenceMessenger.Default.Register<CategoryViewModelTests, CategoryRemovedMessage>(
                this,
                (r, m) => receivedMessage = m);

            await viewModel.RemoveCommand.ExecuteAsync(null);

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Assert.IsNull(await context.Categories.FindAsync(cat.Id));
            }

            Assert.IsNotNull(receivedMessage);
            Assert.AreEqual("ToDelete", receivedMessage.Value.Name);
        }

        [TestMethod]
        public void LoadExistingValue_UpdatesProperties()
        {
            Category cat = new()
            {
                Id = 1,
                Name = "Existing",
                Description = "Existing Description"
            };

            viewModel.LoadExisitngValue(cat);

            Assert.AreEqual("Existing", viewModel.Name);
            Assert.AreEqual("Existing Description", viewModel.Description);
        }
    }
}
