﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Apworks.Events;
using System.Linq;

namespace Apworks.Repositories
{
    public sealed class EventSourcingDomainRepository : EventPublishingDomainRepository
    {
        private readonly IEventStore eventStore;

        public EventSourcingDomainRepository(IEventStore eventStore, IEventPublisher publisher) : base(publisher)
        {
            this.eventStore = eventStore;
        }

        public override TAggregateRoot GetById<TKey, TAggregateRoot>(TKey id)
            => this.GetById<TKey, TAggregateRoot>(id, AggregateRootWithEventSourcing<TKey>.MaxVersion);

        public override TAggregateRoot GetById<TKey, TAggregateRoot>(TKey id, long version)
        {
            var events = this.eventStore.Load<TKey>(typeof(TAggregateRoot).AssemblyQualifiedName, id, sequenceMax: version);
            var aggregateRoot = new TAggregateRoot();
            aggregateRoot.Replay(events.Select(e => e as IDomainEvent));
            return aggregateRoot;
        }

        public override async Task<TAggregateRoot> GetByIdAsync<TKey, TAggregateRoot>(TKey id, CancellationToken cancellationToken)
            => await this.GetByIdAsync<TKey, TAggregateRoot>(id, AggregateRootWithEventSourcing<TKey>.MaxVersion, cancellationToken);

        public override async Task<TAggregateRoot> GetByIdAsync<TKey, TAggregateRoot>(TKey id, long version, CancellationToken cancellationToken)
        {
            var events = await this.eventStore.LoadAsync<TKey>(typeof(TAggregateRoot).AssemblyQualifiedName, 
                id, 
                sequenceMax: version, 
                cancellationToken: cancellationToken) as IEnumerable<IDomainEvent>;

            var aggregateRoot = new TAggregateRoot();
            aggregateRoot.Replay(events);
            return aggregateRoot;
        }

        public override void Save<TKey, TAggregateRoot>(TAggregateRoot aggregateRoot)
        {
            // Saves the uncommitted events to the event store.
            var uncommittedEvents = aggregateRoot.UncommittedEvents;
            this.eventStore.Save(uncommittedEvents); // This will save the uncommitted events in a transaction.

            // Publishes the events.
            this.Publisher.PublishAll(uncommittedEvents);

            // Purges the uncommitted events.
            ((IPurgeable)aggregateRoot).Purge();
        }

        public override async Task SaveAsync<TKey, TAggregateRoot>(TAggregateRoot aggregateRoot, CancellationToken cancellationToken)
        {
            // Saves the uncommitted events to the event store.
            var uncommittedEvents = aggregateRoot.UncommittedEvents;
            await this.eventStore.SaveAsync(uncommittedEvents, cancellationToken); // This will save the uncommitted events in a transaction.

            // Publishes the events.
            await this.Publisher.PublishAllAsync(uncommittedEvents);

            // Purges the uncommitted events.
            ((IPurgeable)aggregateRoot).Purge();
        }
    }
}
