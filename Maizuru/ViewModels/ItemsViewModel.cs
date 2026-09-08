// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Maizuru.Contexts;
using Maizuru.Extensions;
using Maizuru.Messages;
using Maizuru.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maizuru.ViewModels
{
    public partial class ItemsViewModel : ObservableObject
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator;
        private readonly IDbContextFactory<MaizuruContext> dbContextFactory;

        public ItemsViewModel(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, IDbContextFactory<MaizuruContext> dbContextFactory)
        {
            this.embeddingGenerator = embeddingGenerator;
            this.dbContextFactory = dbContextFactory;
            Items = [];
            Categories = [];
            PaymentMethods = [];
        }

        [ObservableProperty]
        public partial ObservableCollection<Item> Items { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<Category?> Categories { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<PaymentMethod?> PaymentMethods { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(FilterCommand))]
        public partial Category? Category { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(FilterCommand))]
        public partial PaymentMethod? PaymentMethod { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(InvokeCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
        public partial Item? Item { get; set; }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task LoadAsync()
        {
            Items.Clear();
            PaymentMethods.Clear();
            Categories.Clear();

            PaymentMethods.Add(null);
            Categories.Add(null);

            using MaizuruContext context = await dbContextFactory.CreateDbContextAsync();

            foreach (
                Item item in
                context.Items
                .Include(x => x.Category)
                .Include(x => x.PaymentMethod)
                .AsSplitQuery()
                .AsEnumerable()
                .OrderByDescending(i => i.DateTimeOffset)
                .ThenBy(i => i.Id)
                .ToList())
                Items.Add(item);

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

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task AddAsync()
        {
            using MaizuruContext context = await dbContextFactory.CreateDbContextAsync();

            Item item = new()
            {
                CategoryId = Categories.OfType<Category>().First().Id,
                PaymentMethodId = PaymentMethods.OfType<PaymentMethod>().First().Id,
            };

            context.Add(item);
            await context.SaveChangesAsync();
            await LoadAsync();
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanInvokeOrRemove))]
        private async Task RemoveAsync()
        {
            if (Item is null)
                return;

            using MaizuruContext context = await dbContextFactory.CreateDbContextAsync();

            context.Remove(Item);
            await context.SaveChangesAsync();
            await LoadAsync();
        }

        [RelayCommand(CanExecute = nameof(CanInvokeOrRemove))]
        private void Invoke()
        {
            if (Item == null)
                return;

            WeakReferenceMessenger.Default.Send(new ItemInvokedMessage(Item));
        }

        private bool CanInvokeOrRemove()
        {
            return Item is not null;
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanFilter))]
        private async Task FilterAsync(string phrase)
        {
            await LoadAsync();

            Item[] items = [.. Items];
            Items.Clear();

            if (!string.IsNullOrWhiteSpace(phrase))
            {
                EmbeddingGenerationOptions options = new()
                {
                    Dimensions = Constants.DIMENSIONS,
                };
                ReadOnlyMemory<float> readOnlyMemory = await embeddingGenerator.GenerateVectorAsync(
                    $"task: search result | query: {phrase}",
                    options);
                float[] vectorS = readOnlyMemory.ToArray();
                items = items
                    .Where(i => i.Vector * vectorS > Constants.MATCH_BORDER_LINE)
                    .ToArray();
            }

            if (Category is Category category)
            {
                items = items
                    .Where(i => i.CategoryId == category.Id)
                    .ToArray();
            }

            if (PaymentMethod is PaymentMethod)
                items = items
                    .Where(i => i.PaymentMethodId == PaymentMethod.Id)
                    .ToArray();

            foreach (
                Item item in
                items)
                Items.Add(item);
        }

        private bool CanFilter(string phrase)
        {
            return !(string.IsNullOrWhiteSpace(phrase) && (Category is null) && (PaymentMethod is null));
        }
    }
}
