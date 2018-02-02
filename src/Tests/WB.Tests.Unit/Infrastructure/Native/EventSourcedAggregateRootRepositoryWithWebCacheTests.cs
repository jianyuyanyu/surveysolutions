﻿using System;
using System.Collections.Generic;
using Moq;
using Ncqrs.Domain.Storage;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Sourcing.Snapshotting;
using Ncqrs.Eventing.Storage;
using NUnit.Framework;
using WB.Core.Infrastructure.Aggregates;
using WB.Infrastructure.Native.Monitoring;
using WB.Infrastructure.Native.Storage;
using WB.Tests.Abc;

namespace WB.Tests.Unit.Infrastructure.Native
{
    public class EventSourcedAggregateRootRepositoryWithWebCacheTests
    {
        [Test]
        public void on_evict_should_not_call_cached_remove_method_twice()
        {
            var eventStore = new Mock<IEventStore>();
            var snapshotStore = new Mock<ISnapshotStore>();
            var domainRepo = new Mock<IDomainRepository>();

            domainRepo
                .Setup(dr => dr.Load(It.IsAny<Type>(), It.IsAny<Guid>(), null, It.IsAny<IEnumerable<CommittedEvent>>()))
                .Returns<Type, Guid, Snapshot, IEnumerable<CommittedEvent>>((type, id, s, ce) => Mock.Of<IEventSourcedAggregateRoot>(ar => ar.EventSourceId == id));

            var repo = new EventSourcedAggregateRootRepositoryWithWebCache(eventStore.Object, snapshotStore.Object, domainRepo.Object,
                new Stub.StubAggregateLock());

            CommonMetrics.StateFullInterviewsCount.Set(0);

            repo.GetLatest(typeof(IEventSourcedAggregateRoot), Id.g1);
            Assert.That(CommonMetrics.StateFullInterviewsCount.Value, Is.EqualTo(1));

            var entity = repo.GetLatest(typeof(IEventSourcedAggregateRoot), Id.g2);
            Assert.That(CommonMetrics.StateFullInterviewsCount.Value, Is.EqualTo(2));
            Assert.NotNull(entity);

            repo.Evict(Id.g1);
            Assert.That(CommonMetrics.StateFullInterviewsCount.Value, Is.EqualTo(1));

            repo.Evict(Id.g1);
            Assert.That(CommonMetrics.StateFullInterviewsCount.Value, Is.EqualTo(1), "Should not decrease interviews counter");
        }

        [Test]
        public void should_evict_from_cache_if_aggregate_root_is_dirty()
        {
            var eventStore    = new Mock<IEventStore>();
            var snapshotStore = new Mock<ISnapshotStore>();
            var domainRepo    = new Mock<IDomainRepository>();

            const int versionForNewObjects = 100;
            const int changedVersion = 200;

            domainRepo
                .Setup(dr =>
                    dr.Load(It.IsAny<Type>(), It.IsAny<Guid>(), null, It.IsAny<IEnumerable<CommittedEvent>>()))
                .Returns<Type, Guid, Snapshot, IEnumerable<CommittedEvent>>((type, id, s, ce)
                    => Mock.Of<IEventSourcedAggregateRoot>(ar => ar.EventSourceId == id && ar.Version == versionForNewObjects));

            var repo = new EventSourcedAggregateRootRepositoryWithWebCache(eventStore.Object, snapshotStore.Object, domainRepo.Object,
                new Stub.StubAggregateLock());

            CommonMetrics.StateFullInterviewsCount.Set(0);
            var entity = repo.GetLatest(typeof(IEventSourcedAggregateRoot), Id.g1);

            Assert.IsNotNull(entity);
            var entityMock = Mock.Get(entity);

            // Apply version change for existing Id.g1 entity and mark it as DIRTY
            entityMock.Setup(e => e.HasUncommittedChanges()).Returns(true);
            entityMock.Setup(e => e.Version).Returns(changedVersion);

            Assert.That(entity.Version, Is.EqualTo(changedVersion), "Just to make sure that entityMock work as expected");

            // act
            entity = repo.GetLatest(typeof(IEventSourcedAggregateRoot), Id.g1);
            
            // assert
            Assert.That(entity.Version, Is.EqualTo(versionForNewObjects), "Dirty entity should be evicted and read from domainRepo");
        }
    }
}