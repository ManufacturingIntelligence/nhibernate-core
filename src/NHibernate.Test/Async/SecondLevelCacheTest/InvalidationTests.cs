﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NHibernate.Cache;
using NHibernate.Cfg;
using NHibernate.Impl;
using NHibernate.Test.SecondLevelCacheTests;
using NSubstitute;
using NUnit.Framework;

namespace NHibernate.Test.SecondLevelCacheTest
{
	using System.Threading.Tasks;
	using System.Threading;
	[TestFixture]
	public class InvalidationTestsAsync : TestCase
	{
		protected override string MappingsAssembly => "NHibernate.Test";

		protected override IList Mappings => new[] { "SecondLevelCacheTest.Item.hbm.xml" };

		protected override void Configure(Configuration configuration)
		{
			configuration.SetProperty(Environment.CacheProvider, typeof(HashtableCacheProvider).AssemblyQualifiedName);
			configuration.SetProperty(Environment.UseQueryCache, "true");
		}

		[Test]
		public async Task InvalidatesEntitiesAsync()
		{
			var debugSessionFactory = (DebugSessionFactory) Sfi;

			var cache = Substitute.For<UpdateTimestampsCache>(Sfi.Settings, new Dictionary<string, string>());

			var updateTimestampsCacheField = typeof(SessionFactoryImpl).GetField(
				"updateTimestampsCache",
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			updateTimestampsCacheField.SetValue(debugSessionFactory.ActualFactory, cache);

			//"Received" assertions can not be used since the collection is reused and cleared between calls.
			//The received args are cloned and stored
			var preInvalidations = new List<IReadOnlyCollection<string>>();
			var invalidations = new List<IReadOnlyCollection<string>>();

			await (cache.PreInvalidateAsync(Arg.Do<IReadOnlyCollection<string>>(x => preInvalidations.Add(x.ToList())), CancellationToken.None));
			await (cache.InvalidateAsync(Arg.Do<IReadOnlyCollection<string>>(x => invalidations.Add(x.ToList())), CancellationToken.None));

			using (var session = OpenSession())
			{
				using (var tx = session.BeginTransaction())
				{
					foreach (var i in Enumerable.Range(1, 10))
					{
						var item = new Item {Id = i};
						await (session.SaveAsync(item));
					}

					await (tx.CommitAsync());
				}

				using (var tx = session.BeginTransaction())
				{
					foreach (var i in Enumerable.Range(1, 10))
					{
						var item = await (session.GetAsync<Item>(i));
						item.Name = item.Id.ToString();
					}

					await (tx.CommitAsync());
				}

				using (var tx = session.BeginTransaction())
				{
					foreach (var i in Enumerable.Range(1, 10))
					{
						var item = await (session.GetAsync<Item>(i));
						await (session.DeleteAsync(item));
					}

					await (tx.CommitAsync());
				}
			}

			//Should receive one preinvalidation and one invalidation per commit
			Assert.That(preInvalidations, Has.Count.EqualTo(3));
			Assert.That(preInvalidations, Has.All.Count.EqualTo(1).And.Contains("Item"));

			Assert.That(invalidations, Has.Count.EqualTo(3));
			Assert.That(invalidations, Has.All.Count.EqualTo(1).And.Contains("Item"));
		}

		protected override void OnTearDown()
		{
			using (var s = OpenSession())
			using (var tx = s.BeginTransaction())
			{
				s.Delete("from Item");
				tx.Commit();
			}
		}
	}
}
