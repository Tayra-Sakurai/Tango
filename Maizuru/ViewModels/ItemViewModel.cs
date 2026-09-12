// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Maizuru.Contexts;
using Maizuru.Messages;
using Maizuru.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maizuru.ViewModels
{
    public partial class ItemViewModel : ObservableValidator
    {
        private readonly IDbContextFactory<MaizuruContext> factory;
        private readonly IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator;
        private Item item;

        public ItemViewModel(IDbContextFactory<MaizuruContext> factory, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
        {
            this.factory = factory;
            this.embeddingGenerator = embeddingGenerator;
            item = new();
            Categories = [];
            PaymentMethods = [];
            ErrorsChanged += ItemViewModel_ErrorsChanged;
        }

        private void ItemViewModel_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
        {
            SaveCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task LoadAsync()
        {
            using MaizuruContext context = await factory.CreateDbContextAsync();

            Categories.Clear();
            PaymentMethods.Clear();

            await foreach (
                Category category in
                context.Categories
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.Categories)
                .ThenInclude(c => c.Categories)
                .ThenInclude(c => c.Categories)
                .ThenInclude(c => c.Categories)
                .AsAsyncEnumerable())
                Categories.Add(category);

            await foreach (
                PaymentMethod paymentMethod in
                context.PaymentMethods
                .AsAsyncEnumerable())
                PaymentMethods.Add(paymentMethod);
        }

        public void InitializeForExistingValue(Item item)
        {
            this.item = item;

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(Date));
            OnPropertyChanged(nameof(Time));
            OnPropertyChanged(nameof(Expense));
            OnPropertyChanged(nameof(Income));
            OnPropertyChanged(nameof(Category));
            OnPropertyChanged(nameof(PaymentMethod));
            ValidateAllProperties();
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            if (HasErrors)
                return;

            item.PaymentMethod = null;
            item.Category = null;

            using MaizuruContext context = await factory.CreateDbContextAsync();

            EmbeddingGenerationOptions options = new()
            {
                Dimensions = Constants.DIMENSIONS,
            };

            if (string.IsNullOrWhiteSpace(Description))
                item.Vector = (await embeddingGenerator.GenerateVectorAsync(
                    $"title: none | text: ${Name}",
                    options))
                    .ToArray();
            else
                item.Vector = (
                    await embeddingGenerator
                    .GenerateVectorAsync(
                        $"title: {Name} | text: {Description}",
                        options))
                        .ToArray();

            context.Update(item);
            await context.SaveChangesAsync();
        }

        private bool CanSave()
        {
            return !HasErrors;
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task RemoveAsync()
        {
            using MaizuruContext context = await factory.CreateDbContextAsync();
            context.Remove(item);
            await context.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(new ItemRemovedMessage(item));
        }

        [ObservableProperty]
        public partial ObservableCollection<Category> Categories { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<PaymentMethod> PaymentMethods { get; set; }

        [Required]
        public string Name
        {
            get => item.Name;
            set => SetProperty(item.Name, value, item, (m, v) => m.Name = v, true);
        }

        public string? Description
        {
            get => item.Description;
            set => SetProperty(item.Description, value, item, (m, v) => m.Description = v, true);
        }

        [CustomValidation(typeof(ItemViewModel), nameof(ValidateDate))]
        public DateTimeOffset? Date
        {
            get => item.DateTimeOffset - item.DateTimeOffset.TimeOfDay;
            set
            {
                if (value is DateTimeOffset date)
                {
                    DateTimeOffset dateOnly = item.DateTimeOffset - item.DateTimeOffset.TimeOfDay;
                    if (dateOnly != (date - date.TimeOfDay))
                    {
                        SetDate(item, date.Date);
                        OnPropertyChanged();
                    }
                    ValidateProperty(new DateTimeOffset(date.Date));
                }
                else
                    ValidateProperty(value);
            }
        }

        [CustomValidation(typeof(ItemViewModel), nameof(ValidateTime))]
        public TimeSpan Time
        {
            get => item.DateTimeOffset.TimeOfDay;
            set
            {
                if (item.DateTimeOffset.TimeOfDay != value)
                {
                    SetTime(item, value);
                    OnPropertyChanged();
                }
                ValidateProperty(value);
            }
        }

        private static void SetDate(Item model, DateTime date)
        {
            TimeSpan timeOfDay = model.DateTimeOffset.TimeOfDay;
            date -= date.TimeOfDay;
            date += timeOfDay;
            model.DateTimeOffset = date;
        }

        private static void SetTime(Item model, TimeSpan timeOfDay)
        {
            model.DateTimeOffset -= model.DateTimeOffset.TimeOfDay;
            model.DateTimeOffset += timeOfDay;
        }

        public static ValidationResult? ValidateDate(DateTimeOffset? dateTimeOffset, ValidationContext context)
        {
            ItemViewModel viewModel = (ItemViewModel)context.ObjectInstance;
            if (dateTimeOffset is DateTimeOffset dateTimeOffset1)
            {
                dateTimeOffset1 -= dateTimeOffset1.TimeOfDay;
                dateTimeOffset1 += viewModel.Time;

                if (dateTimeOffset1 > DateTimeOffset.Now)
                    return new("The time of trade must be in the past.");

                return ValidationResult.Success;
            }
            return new("The date must be selected.");
        }

        public static ValidationResult? ValidateTime(TimeSpan value, ValidationContext context)
        {
            ItemViewModel itemViewModel = (ItemViewModel)context.ObjectInstance;
            DateTimeOffset dateTimeOffset = itemViewModel.Date - itemViewModel.Date.TimeOfDay;
            dateTimeOffset += value;

            if (dateTimeOffset > DateTimeOffset.Now)
                return new("The time of trade must be in the past.");

            return ValidationResult.Success;
        }

        [Required]
        public Category? Category
        {
            get => Categories.FirstOrDefault(c => c.Id == item.CategoryId);
            set
            {
                if (value is not null)
                {
                    if (value.Id != item.CategoryId)
                    {
                        item.CategoryId = value.Id;
                        OnPropertyChanged();
                    }
                }
                ValidateProperty(value, nameof(Category));
            }
        }

        [CustomValidation(typeof(ItemViewModel), nameof(NotNullValidation))]
        public PaymentMethod? PaymentMethod
        {
            get => PaymentMethods.FirstOrDefault(p => p.Id == item.PaymentMethodId);
            set
            {
                if (value is not null)
                {
                    if (value.Id != item.PaymentMethodId)
                    {
                        item.PaymentMethodId = value.Id;
                        OnPropertyChanged();
                    }
                }
                ValidateProperty(value, nameof(PaymentMethod));
            }
        }

        public static ValidationResult? NotNullValidation(PaymentMethod? value, ValidationContext context)
        {
            if (value == null)
                return new($"The property '{context.MemberName ?? string.Empty}' is required.");

            return ValidationResult.Success;
        }

        [Range(0, double.MaxValue)]
        [CustomValidation(typeof(ItemViewModel), nameof(ValidateIncome))]
        public double Income
        {
            get => item.Income;
            set => SetProperty(item.Income, value, item, (m, v) => m.Income = v, true);
        }

        [Range(0, double.MaxValue)]
        [CustomValidation(typeof (ItemViewModel), nameof(ValidateExpense))]
        public double Expense
        {
            get => item.Expense;
            set => SetProperty(item.Expense, value, item, (m, v) => m.Expense = v, true);
        }

        public static ValidationResult? ValidateIncome(double value, ValidationContext context)
        {
            ItemViewModel viewModel = (ItemViewModel)context.ObjectInstance;
            if (value > 0 && viewModel.Expense > 0)
                return new("You must set either income or expense.");

            return ValidationResult.Success;
        }

        public static ValidationResult? ValidateExpense(double value, ValidationContext context)
        {
            ItemViewModel viewModel = (ItemViewModel)context.ObjectInstance;
            if (value > 0 && viewModel.Income > 0)
                return new("You must set either income or expense.");

            return ValidationResult.Success;
        }

        public double SmallChange
        {
            get
            {
                CultureInfo culture = CultureInfo.CurrentCulture;
                int digits = culture.NumberFormat.CurrencyDecimalDigits;
                return Math.Pow(10, -digits);
            }
        }
    }
}
