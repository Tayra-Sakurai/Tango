// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Maizuru.Contexts;
using Maizuru.Messages;
using Maizuru.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace Maizuru.ViewModels
{
    public partial class PaymentMethodsViewModel : ObservableObject
    {
        private readonly IDbContextFactory<MaizuruContext> factory;

        public PaymentMethodsViewModel(IDbContextFactory<MaizuruContext> factory)
        {
            this.factory = factory;
            PaymentMethods = [];
        }

        [ObservableProperty]
        public partial ObservableCollection<PaymentMethod> PaymentMethods { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(InvokeCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
        public partial PaymentMethod? PaymentMethod { get; set; }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task LoadAsync()
        {
            PaymentMethods.Clear();

            using MaizuruContext context = await factory.CreateDbContextAsync();
            await foreach (
                PaymentMethod paymentMethod in
                context.PaymentMethods
                .Include(p => p.Items)
                .AsAsyncEnumerable())
                PaymentMethods.Add(paymentMethod);
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task AddAsync()
        {
            PaymentMethod paymentMethod = new();

            using MaizuruContext context = await factory.CreateDbContextAsync();
            context.Add(paymentMethod);
            await context.SaveChangesAsync();
            await LoadAsync();
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanInvokeOrRemove))]
        private async Task RemoveAsync()
        {
            if (PaymentMethod is null)
                return;

            using MaizuruContext context = await factory.CreateDbContextAsync();
            context.Remove(PaymentMethod);
            await context.SaveChangesAsync();
            await LoadAsync();
        }

        private bool CanInvokeOrRemove()
        {
            return PaymentMethod != null;
        }

        [RelayCommand(CanExecute = nameof(CanInvokeOrRemove))]
        private void Invoke()
        {
            if (PaymentMethod == null)
                return;

            WeakReferenceMessenger.Default.Send(new PaymentMethodInvokedMessage(PaymentMethod));
        }
    }
}
