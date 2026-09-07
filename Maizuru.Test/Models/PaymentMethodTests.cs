// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using Maizuru.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maizuru.Test.Models
{
    [TestClass]
    public class PaymentMethodTests
    {
        [TestMethod]
        public void Balance_EmptyItems_ReturnsZero()
        {
            PaymentMethod paymentMethod = new();

            Assert.AreEqual(0.0, paymentMethod.Balance);
        }

        [TestMethod]
        public void Balance_WithIncomeAndExpense_ReturnsNetSum()
        {
            PaymentMethod paymentMethod = new();
            paymentMethod.Items.Add(new Item { Income = 5000, Expense = 0 });
            paymentMethod.Items.Add(new Item { Income = 0, Expense = 1200 });
            paymentMethod.Items.Add(new Item { Income = 300, Expense = 100 });

            // 5000 - 1200 + (300 - 100) = 4000
            Assert.AreEqual(4000.0, paymentMethod.Balance, 1e-6);
        }

        [TestMethod]
        public void Balance_ExpensesExceedIncome_ReturnsNegative()
        {
            PaymentMethod paymentMethod = new();
            paymentMethod.Items.Add(new Item { Income = 100, Expense = 350 });

            Assert.AreEqual(-250.0, paymentMethod.Balance, 1e-6);
        }

        [TestMethod]
        public void Balance_AddingItem_UpdatesBalanceImmediately()
        {
            PaymentMethod paymentMethod = new();
            Assert.AreEqual(0.0, paymentMethod.Balance);

            paymentMethod.Items.Add(new Item { Income = 1000, Expense = 200 });
            Assert.AreEqual(800.0, paymentMethod.Balance);

            paymentMethod.Items.Add(new Item { Income = 0, Expense = 300 });
            Assert.AreEqual(500.0, paymentMethod.Balance);
        }
    }
}
