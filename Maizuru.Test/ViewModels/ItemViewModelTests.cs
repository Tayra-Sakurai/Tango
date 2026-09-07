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
    public class ItemViewModelTests
    {
        private TestDbContextFactory factory = null!;
        private FakeEmbeddingGenerator embeddingGenerator = null!;
        private ItemViewModel viewModel = null!;
        private Category category = null!;
        private PaymentMethod paymentMethod = null!;

        [TestInitialize]
        public void Initialize()
        {
            factory = new TestDbContextFactory();
            embeddingGenerator = new FakeEmbeddingGenerator();
            viewModel = new ItemViewModel(factory, embeddingGenerator);

            using MaizuruContext context = factory.CreateDbContext();
            category = new Category { Name = "Food" };
            paymentMethod = new PaymentMethod { Name = "Cash" };
            context.Categories.Add(category);
            context.PaymentMethods.Add(paymentMethod);
            context.SaveChanges();

            viewModel.Categories.Add(category);
            viewModel.PaymentMethods.Add(paymentMethod);
        }

        [TestCleanup]
        public void Cleanup()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            factory.Dispose();
        }

        [TestMethod]
        public void Name_Empty_FailsValidation()
        {
            viewModel.Name = "Initial";
            viewModel.Name = "";
            viewModel.Category = category;
            viewModel.PaymentMethod = paymentMethod;

            Assert.IsTrue(viewModel.HasErrors);
            Assert.IsFalse(viewModel.SaveCommand.CanExecute(null));
        }

        [TestMethod]
        public void Category_Null_FailsValidation()
        {
            viewModel.Name = "Valid Name";
            viewModel.Category = null;
            viewModel.PaymentMethod = paymentMethod;

            Assert.IsTrue(viewModel.HasErrors);
            Assert.IsFalse(viewModel.SaveCommand.CanExecute(null));
        }

        [TestMethod]
        public void PaymentMethod_Null_FailsValidation()
        {
            viewModel.Name = "Valid Name";
            viewModel.Category = category;
            viewModel.PaymentMethod = null;

            Assert.IsTrue(viewModel.HasErrors);
            Assert.IsFalse(viewModel.SaveCommand.CanExecute(null));
        }

        [TestMethod]
        public void DateInFuture_FailsValidation()
        {
            viewModel.Name = "Future Purchase";
            viewModel.Category = category;
            viewModel.PaymentMethod = paymentMethod;
            viewModel.Date = DateTimeOffset.Now.AddDays(2);

            Assert.IsTrue(viewModel.HasErrors);
            Assert.IsFalse(viewModel.SaveCommand.CanExecute(null));
        }

        [TestMethod]
        public void BothIncomeAndExpensePositive_FailsValidation()
        {
            viewModel.Name = "Trade";
            viewModel.Category = category;
            viewModel.PaymentMethod = paymentMethod;
            viewModel.Income = 1000;
            viewModel.Expense = 500;

            Assert.IsTrue(viewModel.HasErrors);
            Assert.IsFalse(viewModel.SaveCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task SaveCommand_WhenDescriptionEmpty_GeneratesVectorWithTitleNone()
        {
            float[] returnedVector = new float[Constants.DIMENSIONS];
            returnedVector[0] = 0.88f;
            embeddingGenerator.GeneratorFunc = (prompt, options) => returnedVector;

            viewModel.Name = "Bread";
            viewModel.Description = "";
            viewModel.Category = category;
            viewModel.PaymentMethod = paymentMethod;
            viewModel.Expense = 150;
            viewModel.Date = DateTimeOffset.Now.AddMinutes(-5);

            Assert.IsFalse(viewModel.HasErrors);
            await viewModel.SaveCommand.ExecuteAsync(null);

            Assert.AreEqual(1, embeddingGenerator.Invocations.Count);
            Assert.AreEqual("title: none | text: $Bread", embeddingGenerator.Invocations[0].Prompt);

            using MaizuruContext context = factory.CreateDbContext();
            Item saved = await context.Items.FirstAsync(i => i.Name == "Bread");
            Assert.AreEqual(0.88f, saved.Vector[0], 1e-5f);
        }

        [TestMethod]
        public async Task SaveCommand_WhenDescriptionProvided_GeneratesVectorWithTitleAndDescription()
        {
            float[] returnedVector = new float[Constants.DIMENSIONS];
            returnedVector[0] = 0.77f;
            embeddingGenerator.GeneratorFunc = (prompt, options) => returnedVector;

            viewModel.Name = "Milk";
            viewModel.Description = "Whole milk 1L";
            viewModel.Category = category;
            viewModel.PaymentMethod = paymentMethod;
            viewModel.Expense = 200;
            viewModel.Date = DateTimeOffset.Now.AddMinutes(-5);

            Assert.IsFalse(viewModel.HasErrors);
            await viewModel.SaveCommand.ExecuteAsync(null);

            Assert.AreEqual(1, embeddingGenerator.Invocations.Count);
            Assert.AreEqual("title: Milk | text: Whole milk 1L", embeddingGenerator.Invocations[0].Prompt);

            using MaizuruContext context = factory.CreateDbContext();
            Item saved = await context.Items.FirstAsync(i => i.Name == "Milk");
            Assert.AreEqual(0.77f, saved.Vector[0], 1e-5f);
        }

        [TestMethod]
        public async Task RemoveCommand_RemovesItemAndSendsMessage()
        {
            Item item = new()
            {
                Name = "To Delete",
                CategoryId = category.Id,
                PaymentMethodId = paymentMethod.Id
            };
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.Items.Add(item);
                await context.SaveChangesAsync();
            }

            viewModel.InitializeForExistingValue(item);

            ItemRemovedMessage? received = null;
            WeakReferenceMessenger.Default.Register<ItemViewModelTests, ItemRemovedMessage>(
                this,
                (r, m) => received = m);

            await viewModel.RemoveCommand.ExecuteAsync(null);

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Assert.IsNull(await context.Items.FindAsync(item.Id));
            }

            Assert.IsNotNull(received);
            Assert.AreEqual("To Delete", received.Value.Name);
        }

        [TestMethod]
        public void InitializeForExistingValue_BindsProperties()
        {
            Item item = new()
            {
                Id = 10,
                Name = "Book",
                Description = "Novel",
                Income = 0,
                Expense = 1500,
                CategoryId = category.Id,
                PaymentMethodId = paymentMethod.Id
            };

            viewModel.InitializeForExistingValue(item);

            Assert.AreEqual("Book", viewModel.Name);
            Assert.AreEqual("Novel", viewModel.Description);
            Assert.AreEqual(1500.0, viewModel.Expense);
            Assert.AreEqual(category.Id, viewModel.Category!.Id);
            Assert.AreEqual(paymentMethod.Id, viewModel.PaymentMethod!.Id);
        }
    }
}
