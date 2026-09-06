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

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRemove))]
        private async Task RemoveAsync(PaymentMethod? paymentMethod)
        {
            if (paymentMethod == null)
                return;

            using MaizuruContext context = await factory.CreateDbContextAsync();
            context.Remove(paymentMethod);
            await context.SaveChangesAsync();
            await LoadAsync();
        }

        private static bool CanRemove(PaymentMethod? paymentMethod)
        {
            return paymentMethod is not null;
        }

        [RelayCommand(CanExecute = nameof(CanRemove))]
        private static void Invoke(PaymentMethod? paymentMethod)
        {
            if (paymentMethod == null)
                return;

            WeakReferenceMessenger.Default.Send(new PaymentMethodInvokedMessage(paymentMethod));
        }
    }
}
