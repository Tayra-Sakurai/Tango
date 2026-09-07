// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maizuru.ViewModels
{
    public partial class CategoriesViewModel : ObservableObject
    {
        private readonly IDbContextFactory<MaizuruContext> factory;

        public CategoriesViewModel(IDbContextFactory<MaizuruContext> factory)
        {
            this.factory = factory;
            Categories = [];
        }

        [ObservableProperty]
        public partial ObservableCollection<Category> Categories { get; set; }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task LoadAsync()
        {
            using MaizuruContext context = await factory.CreateDbContextAsync();

            Categories.Clear();

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
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task AddAsync(Category? category)
        {
            using MaizuruContext context = await factory.CreateDbContextAsync();

            if (category is not Category category1)
            {
                Category category2 = new();
                context.Add(category2);
            }
            else
            {
                EntityEntry<Category> entityEntry = context.Attach(category1);
                await entityEntry
                    .Collection(c => c.Categories)
                    .LoadAsync();

                category1.Categories.Add(new());
            }
            await context.SaveChangesAsync();
            await LoadAsync();
        }

        [RelayCommand(CanExecute = nameof(CanInvoke))]
        private void Invoke(Category? category)
        {
            if (category is null)
                return;

            WeakReferenceMessenger.Default.Send(new CategoryInvokedMessage(category));
        }
        
        private static bool CanInvoke(Category? category)
        {
            return category is not null;
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanInvoke))]
        private async Task RemoveAsync(Category? category)
        {
            if (category is null)
                return;

            using MaizuruContext context = await factory.CreateDbContextAsync();
            context.Remove(category);
            await context.SaveChangesAsync();
            await LoadAsync();
        }
    }
}
