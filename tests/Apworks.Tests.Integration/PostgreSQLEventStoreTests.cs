﻿using Apworks.Events;
using Apworks.EventStore.AdoNet;
using Apworks.EventStore.PostgreSQL;
using Apworks.KeyGeneration;
using Apworks.Serialization.Json;
using Apworks.Tests.Integration.Fixtures;
using Apworks.Tests.Integration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Apworks.Tests.Integration
{
    public class PostgreSQLEventStoreTests : IClassFixture<PostgreSQLFixture>, IDisposable
    {
        private readonly PostgreSQLFixture fixture;

        public PostgreSQLEventStoreTests(PostgreSQLFixture fixture)
        {
            this.fixture = fixture;
        }

        public void Dispose()
        {
            this.fixture.ClearTable();
        }

        [Fact]
        public void SaveEventsTest()
        {
            var aggregateRootId = Guid.NewGuid();
            var employee = new Employee { Id = aggregateRootId };

            var event1 = new NameChangedEvent("daxnet");
            var event2 = new TitleChangedEvent("title");
            var event3 = new RegisteredEvent();

            event1.AttachTo(employee);
            event2.AttachTo(employee);
            event3.AttachTo(employee);

            var storeConfig = new AdoNetEventStoreConfiguration(PostgreSQLFixture.ConnectionString, new GuidKeyGenerator());
            var payloadSerializer = new ObjectJsonSerializer();
            var store = new PostgreSqlEventStore(storeConfig, payloadSerializer);
            store.Save(new List<DomainEvent>
            {
                event1,
                event2,
                event3
            });
        }

        [Fact]
        public void LoadEventsTest()
        {
            var aggregateRootId = Guid.NewGuid();
            var employee = new Employee { Id = aggregateRootId };

            var event1 = new NameChangedEvent("daxnet");
            var event2 = new TitleChangedEvent("title");
            var event3 = new RegisteredEvent();

            event1.AttachTo(employee);
            event2.AttachTo(employee);
            event3.AttachTo(employee);

            var storeConfig = new AdoNetEventStoreConfiguration(PostgreSQLFixture.ConnectionString, new GuidKeyGenerator());
            var payloadSerializer = new ObjectJsonSerializer();
            var store = new PostgreSqlEventStore(storeConfig, payloadSerializer);
            store.Save(new List<DomainEvent>
            {
                event1,
                event2,
                event3
            });

            var events = store.Load<Guid>(typeof(Employee).AssemblyQualifiedName, aggregateRootId);
            Assert.Equal(3, events.Count());
        }

        [Fact]
        public void LoadMinimalSequenceEventsTest()
        {
            var aggregateRootId = Guid.NewGuid();
            var employee = new Employee { Id = aggregateRootId };

            var event1 = new NameChangedEvent("daxnet") { Sequence = 1 };
            var event2 = new TitleChangedEvent("title") { Sequence = 2 };
            var event3 = new RegisteredEvent() { Sequence = 3 };

            event1.AttachTo(employee);
            event2.AttachTo(employee);
            event3.AttachTo(employee);

            var storeConfig = new AdoNetEventStoreConfiguration(PostgreSQLFixture.ConnectionString, new GuidKeyGenerator());
            var payloadSerializer = new ObjectJsonSerializer();
            var store = new PostgreSqlEventStore(storeConfig, payloadSerializer);
            store.Save(new List<DomainEvent>
            {
                event1,
                event2,
                event3
            });

            var events = store.Load<Guid>(typeof(Employee).AssemblyQualifiedName, aggregateRootId, 2).ToList();
            Assert.Equal(2, events.Count);
            Assert.IsType(typeof(TitleChangedEvent), events[0]);
            Assert.IsType(typeof(RegisteredEvent), events[1]);
        }

        [Fact]
        public void LoadMaximumSequenceEventsTest()
        {
            var aggregateRootId = Guid.NewGuid();
            var employee = new Employee { Id = aggregateRootId };

            var event1 = new NameChangedEvent("daxnet") { Sequence = 1 };
            var event2 = new TitleChangedEvent("title") { Sequence = 2 };
            var event3 = new RegisteredEvent() { Sequence = 3 };

            event1.AttachTo(employee);
            event2.AttachTo(employee);
            event3.AttachTo(employee);

            var storeConfig = new AdoNetEventStoreConfiguration(PostgreSQLFixture.ConnectionString, new GuidKeyGenerator());
            var payloadSerializer = new ObjectJsonSerializer();
            var store = new PostgreSqlEventStore(storeConfig, payloadSerializer);
            store.Save(new List<DomainEvent>
            {
                event1,
                event2,
                event3
            });

            var events = store.Load<Guid>(typeof(Employee).AssemblyQualifiedName, aggregateRootId, sequenceMax: 2).ToList();
            Assert.Equal(2, events.Count);
            Assert.IsType(typeof(NameChangedEvent), events[0]);
            Assert.IsType(typeof(TitleChangedEvent), events[1]);
        }

        [Fact]
        public void LoadEventsWithMinMaxSequenceTest()
        {
            var aggregateRootId = Guid.NewGuid();
            var employee = new Employee { Id = aggregateRootId };

            var event1 = new NameChangedEvent("daxnet") { Sequence = 1 };
            var event2 = new TitleChangedEvent("title") { Sequence = 2 };
            var event3 = new RegisteredEvent() { Sequence = 3 };

            event1.AttachTo(employee);
            event2.AttachTo(employee);
            event3.AttachTo(employee);

            var storeConfig = new AdoNetEventStoreConfiguration(PostgreSQLFixture.ConnectionString, new GuidKeyGenerator());
            var payloadSerializer = new ObjectJsonSerializer();
            var store = new PostgreSqlEventStore(storeConfig, payloadSerializer);
            store.Save(new List<DomainEvent>
            {
                event1,
                event2,
                event3
            });

            var events = store.Load<Guid>(typeof(Employee).AssemblyQualifiedName, aggregateRootId, 1, 3).ToList();
            Assert.Equal(3, events.Count);
            Assert.IsType(typeof(NameChangedEvent), events[0]);
            Assert.IsType(typeof(TitleChangedEvent), events[1]);
            Assert.IsType(typeof(RegisteredEvent), events[2]);
        }
    }
}
