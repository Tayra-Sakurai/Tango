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

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
        [NotifyCanExecuteChangedFor(nameof(InvokeCommand))]
        public partial Category? Category { get; set; }

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
        private async Task AddAsync()
        {
            using MaizuruContext context = await factory.CreateDbContextAsync();

            if (Category is not Category category1)
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

                Stack<Category> stack = new();
                stack.Push(category1);

                bool toRoot = false;

                for (int count = 0; stack.Count > 0; count++)
                {
                    if (count >= 3)
                    {
                        toRoot = true;
                        break;
                    }

                    Category category2 = stack.Pop();
                    await context.Entry(category2)
                        .Reference(c => c.ParentCategory)
                        .LoadAsync();

                    if (category2.ParentCategory is Category)
                        stack.Push(category2.ParentCategory);
                }

                if (!toRoot)
                    category1.Categories.Add(new());
                else
                    context.Add(new Category());
            }
            await context.SaveChangesAsync();
            await LoadAsync();
        }

        [RelayCommand(CanExecute = nameof(CanInvokeOrRemove))]
        private void Invoke()
        {
            if (Category is null)
                return;

            WeakReferenceMessenger.Default.Send(new CategoryInvokedMessage(Category));
        }
        
        private bool CanInvokeOrRemove()
        {
            return Category is not null;
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanInvokeOrRemove))]
        private async Task RemoveAsync()
        {
            if (Category is null)
                return;

            using MaizuruContext context = await factory.CreateDbContextAsync();
            context.Remove(Category);
            await context.SaveChangesAsync();
            await LoadAsync();
        }
    }
}
