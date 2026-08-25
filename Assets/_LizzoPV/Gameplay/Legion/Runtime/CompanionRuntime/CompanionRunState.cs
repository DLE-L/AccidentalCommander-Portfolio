using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRunLifecycleState
    {
        internal void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CompanionRunModule));
        }

        internal bool TryDispose()
        {
            if (IsDisposed)
                return false;
            IsDisposed = true;
            return true;
        }

        internal bool IsDisposed { get; private set; }
    }

    internal sealed class CompanionRunRequestSequenceState
    {
        internal long LastCommand { get; private set; }
        internal long LastAdvance { get; private set; }

        internal bool CanAcceptCommand(long sequence) => sequence > LastCommand;
        internal bool CanAcceptAdvance(long sequence) => sequence > LastAdvance;
        internal void AcceptCommand(long sequence) => LastCommand = sequence;
        internal void AcceptAdvance(long sequence) => LastAdvance = sequence;

        internal void Reset()
        {
            LastCommand = 0L;
            LastAdvance = 0L;
        }
    }

    internal sealed class CompanionRunEventJournal
    {
        readonly List<CompanionRunEvent> _events = new List<CompanionRunEvent>(4);
        long _nextOrder;

        internal long NextOrder()
        {
            _nextOrder += 1L;
            return _nextOrder;
        }

        internal void Add(in CompanionRunEvent runEvent) => _events.Add(runEvent);

        internal IReadOnlyList<CompanionRunEvent> Drain()
        {
            CompanionRunEvent[] drained = _events.ToArray();
            _events.Clear();
            return drained;
        }

        internal void Reset()
        {
            _events.Clear();
            _nextOrder = 0L;
        }
    }
}
