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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maizuru.ViewModels
{
    public partial class CategoryViewModel : ObservableValidator
    {
        private Category category;
        private readonly IDbContextFactory<MaizuruContext> factory;

        [ObservableProperty]
        public partial ObservableCollection<Category?> Categories { get; set; }

        public CategoryViewModel(IDbContextFactory<MaizuruContext> factory)
        {
            this.factory = factory;
            Categories = [];
            category = new();
            ErrorsChanged += CategoryViewModel_ErrorsChanged;
        }

        private void CategoryViewModel_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
        {
            SaveCommand.NotifyCanExecuteChanged();
        }

        public void LoadExisitngValue(Category category)
        {
            this.category = category;

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(ParentCategory));
            ValidateAllProperties();
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task LoadAsync()
        {
            Categories.Clear();
            Categories.Add(null);

            using MaizuruContext context = await factory.CreateDbContextAsync();
            await foreach (
                Category category in
                context.Categories
                .Include(c => c.Categories)
                .ThenInclude(c => c.Categories)
                .ThenInclude(c => c.Categories)
                .Where(c => c.ParentCategoryId == null)
                .AsAsyncEnumerable())
                Categories.Add(category);
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            if (HasErrors)
                return;

            using MaizuruContext context = await factory.CreateDbContextAsync();

            context.Update(category);
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

            context.Remove(category);
            await context.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(new CategoryRemovedMessage(category));
        }

        [Required]
        public string Name
        {
            get => category.Name;
            set => SetProperty(category.Name, value, category, (m, v) => m.Name = v, true);
        }

        public string? Description
        {
            get => category.Description ?? string.Empty;
            set => SetProperty(category.Description, value, category, (m, v) => m.Description = v, true);
        }

        [CustomValidation(typeof(CategoryViewModel), nameof(ValidateParent))]
        public Category? ParentCategory
        {
            get => Categories.FirstOrDefault(c => c?.Id == category.ParentCategoryId);
            set
            {
                if (value?.Id == category.Id)
                {
                    category.ParentCategoryId = null;
                    OnPropertyChanged();
                    ValidateProperty(null, nameof(ParentCategory));
                    return;
                }
                if (category.ParentCategoryId != value?.Id)
                {
                    category.ParentCategoryId = value?.Id;
                    OnPropertyChanged();
                    ValidateProperty(value, nameof(ParentCategory));
                }
            }
        }

        public static ValidationResult? ValidateParent(Category? category, ValidationContext context)
        {
            using MaizuruContext context1 = ((CategoryViewModel)context.ObjectInstance).factory.CreateDbContext();

            if (category == null)
                return ValidationResult.Success;

            Stack<Category> categories = new Stack<Category>();
            categories.Push(category);

            int count = 0;

            while (categories.Count > 0)
            {
                if (count >= Constants.MAX_CATEGORY_LEVELS)
                    return new("Too many category level detected.");

                Category parent = categories.Pop();

                context1.Attach(parent)
                    .Reference(c => c.ParentCategory)
                    .Load();

                if (parent.ParentCategory is Category c)
                {
                    categories.Push(c);
                    count++;
                }
            }

            return ValidationResult.Success;
        }
    }
}
