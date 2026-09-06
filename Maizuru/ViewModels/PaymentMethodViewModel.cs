// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Maizuru.Contexts;
using Maizuru.Messages;
using Maizuru.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Maizuru.ViewModels
{
    public partial class PaymentMethodViewModel : ObservableValidator
    {
        private readonly IDbContextFactory<MaizuruContext> dbContextFactory;
        private PaymentMethod paymentMethod;

        public PaymentMethodViewModel(IDbContextFactory<MaizuruContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory;
            paymentMethod = new();
            ErrorsChanged += PaymentMethodViewModel_ErrorsChanged;
        }

        private void PaymentMethodViewModel_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
        {
            SaveCommand.NotifyCanExecuteChanged();
        }

        public async Task GetExistingValue(PaymentMethod paymentMethod)
        {
            this.paymentMethod = paymentMethod;

            using MaizuruContext context = await dbContextFactory.CreateDbContextAsync();

            EntityEntry<PaymentMethod> entityEntry = context.Attach(paymentMethod);
            await entityEntry
                .Collection(e => e.Items)
                .LoadAsync();

            OnPropertyChanged(nameof(Balance));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));

            ValidateAllProperties();
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            using MaizuruContext context = await dbContextFactory.CreateDbContextAsync();
            context.Update(paymentMethod);
            await context.SaveChangesAsync();
        }

        private bool CanSave()
        {
            return !HasErrors;
        }

        public double Balance => paymentMethod.Balance;

        [Required]
        public string Name
        {
            get => paymentMethod.Name;
            set => SetProperty(paymentMethod.Name, value, paymentMethod, (m, v) => m.Name = v, true);
        }

        public string? Description
        {
            get => paymentMethod.Description;
            set => SetProperty(paymentMethod.Description, value, paymentMethod, (m, v) => m.Description = v, true);
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task RemoveAsync()
        {
            using MaizuruContext context = await dbContextFactory.CreateDbContextAsync();

            context.Remove(paymentMethod);
            await context.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(new PaymentMethodRemovedMessage(paymentMethod));
        }
    }
}
