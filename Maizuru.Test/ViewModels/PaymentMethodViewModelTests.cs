// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

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
    public class PaymentMethodViewModelTests
    {
        private TestDbContextFactory factory = null!;
        private PaymentMethodViewModel viewModel = null!;

        [TestInitialize]
        public void Initialize()
        {
            factory = new TestDbContextFactory();
            viewModel = new PaymentMethodViewModel(factory);
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
            viewModel.Name = "Credit Card";

            Assert.IsFalse(viewModel.HasErrors);
            Assert.IsTrue(viewModel.SaveCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task SaveCommand_PersistsChanges()
        {
            viewModel.Name = "Bank Account";
            viewModel.Description = "Main checking";

            await viewModel.SaveCommand.ExecuteAsync(null);

            using MaizuruContext context = factory.CreateDbContext();
            PaymentMethod saved = await context.PaymentMethods.FirstAsync(p => p.Name == "Bank Account");
            Assert.AreEqual("Main checking", saved.Description);
        }

        [TestMethod]
        public async Task RemoveCommand_RemovesFromDbAndSendsMessage()
        {
            PaymentMethod pm = new() { Name = "PayPay" };
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.PaymentMethods.Add(pm);
                await context.SaveChangesAsync();
            }

            await viewModel.GetExistingValue(pm);

            PaymentMethodRemovedMessage? received = null;
            WeakReferenceMessenger.Default.Register<PaymentMethodViewModelTests, PaymentMethodRemovedMessage>(
                this,
                (r, m) => received = m);

            await viewModel.RemoveCommand.ExecuteAsync(null);

            using (MaizuruContext context = factory.CreateDbContext())
            {
                Assert.IsNull(await context.PaymentMethods.FindAsync(pm.Id));
            }

            Assert.IsNotNull(received);
            Assert.AreEqual("PayPay", received.Value.Name);
        }

        [TestMethod]
        public async Task GetExistingValue_LoadsItemsAndReflectsBalance()
        {
            int pmId;
            using (MaizuruContext context = factory.CreateDbContext())
            {
                Category cat = new() { Name = "General" };
                context.Categories.Add(cat);
                PaymentMethod pm = new() { Name = "Wallet", Description = "Cash in wallet" };
                context.PaymentMethods.Add(pm);
                await context.SaveChangesAsync();

                context.Items.Add(new Item
                {
                    Name = "Salary",
                    Income = 10000,
                    CategoryId = cat.Id,
                    PaymentMethodId = pm.Id
                });
                context.Items.Add(new Item
                {
                    Name = "Lunch",
                    Expense = 1500,
                    CategoryId = cat.Id,
                    PaymentMethodId = pm.Id
                });
                await context.SaveChangesAsync();
                pmId = pm.Id;
            }

            using (MaizuruContext context = factory.CreateDbContext())
            {
                PaymentMethod pm = await context.PaymentMethods.FirstAsync(p => p.Id == pmId);
                await viewModel.GetExistingValue(pm);
            }

            Assert.AreEqual("Wallet", viewModel.Name);
            Assert.AreEqual("Cash in wallet", viewModel.Description);
            Assert.AreEqual(8500.0, viewModel.Balance);
        }
    }
}
