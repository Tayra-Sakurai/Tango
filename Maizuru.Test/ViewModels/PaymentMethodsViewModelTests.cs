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
    public class PaymentMethodsViewModelTests
    {
        private TestDbContextFactory factory = null!;
        private PaymentMethodsViewModel viewModel = null!;

        [TestInitialize]
        public void Initialize()
        {
            factory = new TestDbContextFactory();
            viewModel = new PaymentMethodsViewModel(factory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            factory.Dispose();
        }

        [TestMethod]
        public async Task LoadAsync_LoadsAllPaymentMethodsWithItems()
        {
            using (MaizuruContext context = factory.CreateDbContext())
            {
                Category cat = new() { Name = "General" };
                context.Categories.Add(cat);
                PaymentMethod pm = new() { Name = "Debit" };
                context.PaymentMethods.Add(pm);
                await context.SaveChangesAsync();

                context.Items.Add(new Item
                {
                    Name = "Coffee",
                    Expense = 400,
                    CategoryId = cat.Id,
                    PaymentMethodId = pm.Id
                });
                await context.SaveChangesAsync();
            }

            await viewModel.LoadAsync();

            Assert.AreEqual(1, viewModel.PaymentMethods.Count);
            Assert.AreEqual("Debit", viewModel.PaymentMethods[0].Name);
            Assert.AreEqual(-400.0, viewModel.PaymentMethods[0].Balance);
        }

        [TestMethod]
        public async Task AddCommand_AddsNewPaymentMethodAndReloads()
        {
            await viewModel.AddCommand.ExecuteAsync(null);

            Assert.AreEqual(1, viewModel.PaymentMethods.Count);
            using MaizuruContext context = factory.CreateDbContext();
            Assert.AreEqual(1, await context.PaymentMethods.CountAsync());
        }

        [TestMethod]
        public async Task RemoveCommand_RemovesPaymentMethodAndReloads()
        {
            PaymentMethod pm = new() { Name = "Cash" };
            using (MaizuruContext context = factory.CreateDbContext())
            {
                context.PaymentMethods.Add(pm);
                await context.SaveChangesAsync();
            }

            await viewModel.LoadAsync();
            Assert.AreEqual(1, viewModel.PaymentMethods.Count);

            await viewModel.RemoveCommand.ExecuteAsync(pm);

            Assert.AreEqual(0, viewModel.PaymentMethods.Count);
            using MaizuruContext verifyContext = factory.CreateDbContext();
            Assert.AreEqual(0, await verifyContext.PaymentMethods.CountAsync());
        }

        [TestMethod]
        public void InvokeCommand_SendsPaymentMethodInvokedMessage()
        {
            PaymentMethod pm = new() { Id = 1, Name = "Suica" };
            PaymentMethodInvokedMessage? received = null;

            WeakReferenceMessenger.Default.Register<PaymentMethodsViewModelTests, PaymentMethodInvokedMessage>(
                this,
                (r, m) => received = m);

            Assert.IsTrue(viewModel.InvokeCommand.CanExecute(pm));
            viewModel.InvokeCommand.Execute(pm);

            Assert.IsNotNull(received);
            Assert.AreEqual("Suica", received.Value.Name);
        }

        [TestMethod]
        public void InvokeCommand_WithNull_CannotExecute()
        {
            Assert.IsFalse(viewModel.InvokeCommand.CanExecute(null));
        }
    }
}
