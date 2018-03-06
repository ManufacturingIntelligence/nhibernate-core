﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;

using NHibernate.Classic;
using NHibernate.Engine;
using NHibernate.Impl;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;

namespace NHibernate.Event.Default
{
	using System.Threading.Tasks;
	using System.Threading;
	public partial class DefaultSaveOrUpdateEventListener : AbstractSaveEventListener, ISaveOrUpdateEventListener
	{

		public virtual async Task OnSaveOrUpdateAsync(SaveOrUpdateEvent @event, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ISessionImplementor source = @event.Session;
			object obj = @event.Entity;
			object requestedId = @event.RequestedId;

			if (requestedId != null)
			{
				//assign the requested id to the proxy, *before* 
				//reassociating the proxy
				if (obj.IsProxy())
				{
					((INHibernateProxy)obj).HibernateLazyInitializer.Identifier = requestedId;
				}
			}

			if (ReassociateIfUninitializedProxy(obj, source))
			{
				log.Debug("reassociated uninitialized proxy");
				// an uninitialized proxy, noop, don't even need to 
				// return an id, since it is never a save()
			}
			else
			{
				//initialize properties of the event:
				object entity = await (source.PersistenceContext.UnproxyAndReassociateAsync(obj, cancellationToken)).ConfigureAwait(false);
				@event.Entity = entity;
				@event.Entry = source.PersistenceContext.GetEntry(entity);
				//return the id in the event object
				@event.ResultId = await (PerformSaveOrUpdateAsync(@event, cancellationToken)).ConfigureAwait(false);
			}
		}

		protected virtual async Task<object> PerformSaveOrUpdateAsync(SaveOrUpdateEvent @event, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			EntityState entityState = await (GetEntityStateAsync(@event.Entity, @event.EntityName, @event.Entry, @event.Session, cancellationToken)).ConfigureAwait(false);

			switch (entityState)
			{
				case EntityState.Detached:
					await (EntityIsDetachedAsync(@event, cancellationToken)).ConfigureAwait(false);
					return null;

				case EntityState.Persistent:
					return EntityIsPersistent(@event);

				default:  //TRANSIENT or DELETED
					return await (EntityIsTransientAsync(@event, cancellationToken)).ConfigureAwait(false);
			}
		}

		/// <summary> 
		/// The given save-update event named a transient entity.
		/// Here, we will perform the save processing. 
		/// </summary>
		/// <param name="event">The save event to be handled. </param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns> The entity's identifier after saving. </returns>
		protected virtual async Task<object> EntityIsTransientAsync(SaveOrUpdateEvent @event, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			log.Debug("saving transient instance");

			IEventSource source = @event.Session;
			EntityEntry entityEntry = @event.Entry;
			if (entityEntry != null)
			{
				if (entityEntry.Status == Status.Deleted)
				{
					await (source.ForceFlushAsync(entityEntry, cancellationToken)).ConfigureAwait(false);
				}
				else
				{
					throw new AssertionFailure("entity was persistent");
				}
			}

			object id = await (SaveWithGeneratedOrRequestedIdAsync(@event, cancellationToken)).ConfigureAwait(false);

			source.PersistenceContext.ReassociateProxy(@event.Entity, id);

			return id;
		}

		/// <summary> 
		/// Save the transient instance, assigning the right identifier 
		/// </summary>
		/// <param name="event">The initiating event. </param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns> The entity's identifier value after saving.</returns>
		protected virtual Task<object> SaveWithGeneratedOrRequestedIdAsync(SaveOrUpdateEvent @event, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				if (@event.RequestedId == null)
				{
					return SaveWithGeneratedIdAsync(@event.Entity, @event.EntityName, null, @event.Session, true, cancellationToken);
				}
				else
				{
					return SaveWithRequestedIdAsync(@event.Entity, @event.RequestedId, @event.EntityName, null, @event.Session, cancellationToken);
				}
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <summary> 
		/// The given save-update event named a detached entity.
		/// Here, we will perform the update processing. 
		/// </summary>
		/// <param name="event">The update event to be handled. </param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		protected virtual Task EntityIsDetachedAsync(SaveOrUpdateEvent @event, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				log.Debug("updating detached instance");

				if (@event.Session.PersistenceContext.IsEntryFor(@event.Entity))
				{
					//TODO: assertion only, could be optimized away
					return Task.FromException<object>(new AssertionFailure("entity was persistent"));
				}

				object entity = @event.Entity;

				IEntityPersister persister = @event.Session.GetEntityPersister(@event.EntityName, entity);

				@event.RequestedId = GetUpdateId(entity, persister, @event.RequestedId);

				return PerformUpdateAsync(@event, entity, persister, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		protected virtual async Task PerformUpdateAsync(SaveOrUpdateEvent @event, object entity, IEntityPersister persister, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!persister.IsMutable)
			{
				log.Debug("immutable instance passed to PerformUpdate(), locking");
			}

			if (log.IsDebugEnabled())
			{
				log.Debug("updating {0}", MessageHelper.InfoString(persister, @event.RequestedId, @event.Session.Factory));
			}

			IEventSource source = @event.Session;

			EntityKey key = source.GenerateEntityKey(@event.RequestedId, persister);

			source.PersistenceContext.CheckUniqueness(key, entity);

			if (InvokeUpdateLifecycle(entity, persister, source))
			{
				await (ReassociateAsync(@event, @event.Entity, @event.RequestedId, persister, cancellationToken)).ConfigureAwait(false);
				return;
			}

			// this is a transient object with existing persistent state not loaded by the session
			await (new OnUpdateVisitor(source, @event.RequestedId, entity).ProcessAsync(entity, persister, cancellationToken)).ConfigureAwait(false);

			//TODO: put this stuff back in to read snapshot from
			//      the second-level cache (needs some extra work)
			/*Object[] cachedState = null;
			
			if ( persister.hasCache() ) {
			CacheEntry entry = (CacheEntry) persister.getCache()
			.get( event.getRequestedId(), source.getTimestamp() );
			cachedState = entry==null ? 
			null : 
			entry.getState(); //TODO: half-assemble this stuff
			}*/

			source.PersistenceContext.AddEntity(
				entity, 
				persister.IsMutable ? Status.Loaded : Status.ReadOnly,
				null, 
				key,
				persister.GetVersion(entity), 
				LockMode.None, 
				true, 
				persister,
				false,
				true);

			//persister.AfterReassociate(entity, source); TODO H3.2 not ported

			if (log.IsDebugEnabled())
			{
				log.Debug("updating {0}", MessageHelper.InfoString(persister, @event.RequestedId, source.Factory));
			}

			await (CascadeOnUpdateAsync(@event, persister, entity, cancellationToken)).ConfigureAwait(false);
		}

		/// <summary> 
		/// Handles the calls needed to perform cascades as part of an update request
		/// for the given entity. 
		/// </summary>
		/// <param name="event">The event currently being processed. </param>
		/// <param name="persister">The defined persister for the entity being updated. </param>
		/// <param name="entity">The entity being updated. </param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		private async Task CascadeOnUpdateAsync(SaveOrUpdateEvent @event, IEntityPersister persister, object entity, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			IEventSource source = @event.Session;
			source.PersistenceContext.IncrementCascadeLevel();
			try
			{
				await (new Cascade(CascadingAction.SaveUpdate, CascadePoint.AfterUpdate, source).CascadeOnAsync(persister, entity, cancellationToken)).ConfigureAwait(false);
			}
			finally
			{
				source.PersistenceContext.DecrementCascadeLevel();
			}
		}
	}
}
