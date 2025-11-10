using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using ServiceLayer.Statistic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class GetUserProSummaryTest
    {
        private readonly Mock<LuminaSystemContext> _mockContext;
        private readonly Mock<IStatisticService> _mockStatisticService;
        private readonly StatisticController _controller;

        public GetUserProSummaryTest()
        {
            _mockContext = new Mock<LuminaSystemContext>();
            _mockStatisticService = new Mock<IStatisticService>();
            _controller = new StatisticController(_mockContext.Object, _mockStatisticService.Object);
        }

        #region GetUserProSummary Tests

        [Fact]
        public async Task GetUserProSummary_ValidUserId_ReturnsOkWithData()
        {
            // Arrange
            int userId = 1;
            var proPackageIds = new int[] { 1, 2, 3 };
            
            // Setup Payments
            var payments = new List<Payment>
            {
                new Payment 
                { 
                    PaymentId = 1, 
                    UserId = userId, 
                    Status = "Success", 
                    Amount = 100000,
                    PackageId = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Payment 
                { 
                    PaymentId = 2, 
                    UserId = userId, 
                    Status = "Success", 
                    Amount = 250000,
                    PackageId = 2,
                    CreatedAt = DateTime.UtcNow
                }
            };
            var mockPaymentSet = CreateMockDbSet(payments.AsQueryable());
            _mockContext.Setup(c => c.Payments).Returns(mockPaymentSet.Object);

            // Setup Subscriptions
            var now = DateTime.UtcNow;
            var subscriptions = new List<Subscription>
            {
                new Subscription 
                { 
                    SubscriptionId = 1, 
                    UserId = userId,
                    PackageId = 1,
                    Status = "Active",
                    StartTime = now.AddDays(-10),
                    EndTime = now.AddDays(20)
                },
                new Subscription 
                { 
                    SubscriptionId = 2, 
                    UserId = userId,
                    PackageId = 2,
                    Status = "Active",
                    StartTime = now.AddDays(-30),
                    EndTime = now.AddDays(60)
                }
            };
            var mockSubscriptionSet = CreateMockDbSet(subscriptions.AsQueryable());
            _mockContext.Setup(c => c.Subscriptions).Returns(mockSubscriptionSet.Object);

            // Act
            var result = await _controller.GetUserProSummary(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            // Verify response structure
            var response = okResult.Value;
            Assert.NotNull(response.GetType().GetProperty("totalMoney"));
            Assert.NotNull(response.GetType().GetProperty("totalPackages"));
            Assert.NotNull(response.GetType().GetProperty("totalDays"));
            Assert.NotNull(response.GetType().GetProperty("remainDays"));
            
            // Verify values
            var totalMoneyProperty = response.GetType().GetProperty("totalMoney");
            var totalPackagesProperty = response.GetType().GetProperty("totalPackages");
            Assert.Equal(350000m, totalMoneyProperty.GetValue(response));
            Assert.Equal(2, totalPackagesProperty.GetValue(response));
        }

        [Fact]
        public async Task GetUserProSummary_UserNotFound_ReturnsOkWithZeroData()
        {
            // Arrange
            int userId = 999;
            
            var emptyPayments = new List<Payment>();
            var mockPaymentSet = CreateMockDbSet(emptyPayments.AsQueryable());
            _mockContext.Setup(c => c.Payments).Returns(mockPaymentSet.Object);

            var emptySubscriptions = new List<Subscription>();
            var mockSubscriptionSet = CreateMockDbSet(emptySubscriptions.AsQueryable());
            _mockContext.Setup(c => c.Subscriptions).Returns(mockSubscriptionSet.Object);

            // Act
            var result = await _controller.GetUserProSummary(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            
            var totalMoneyProperty = response.GetType().GetProperty("totalMoney");
            var totalPackagesProperty = response.GetType().GetProperty("totalPackages");
            var totalDaysProperty = response.GetType().GetProperty("totalDays");
            var remainDaysProperty = response.GetType().GetProperty("remainDays");
            
            Assert.Equal(0m, totalMoneyProperty.GetValue(response));
            Assert.Equal(0, totalPackagesProperty.GetValue(response));
            Assert.Equal(0, totalDaysProperty.GetValue(response));
            Assert.Equal(0, remainDaysProperty.GetValue(response));
        }

        #endregion

        private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            
            mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
            
            return mockSet;
        }
    }

    // Helper classes for async operations
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(
                    name: nameof(IQueryProvider.Execute),
                    genericParameterCount: 1,
                    types: new[] { typeof(Expression) })
                .MakeGenericMethod(resultType)
                .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(resultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }

        public T Current => _inner.Current;
    }
}