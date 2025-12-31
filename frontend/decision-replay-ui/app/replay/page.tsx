'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { useReplayStore } from '@/lib/store';
import { decisionService } from '@/lib/api';
import { TimelineScrubber } from '@/components/replay/timeline-scrubber';
import { EventInspector } from '@/components/replay/event-inspector';
import { ChartsPanel } from '@/components/replay/charts-panel';
import { Button } from '@/components/ui/button';
import { ArrowLeft, Loader2 } from 'lucide-react';
import Link from 'next/link';

export default function ReplayPage() {
  const searchParams = useSearchParams();
  const decisionId = searchParams.get('id');
  
  const { loadReplay, isLoading, error, setLoading, setError } = useReplayStore();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!decisionId || !mounted) return;

    const loadDecisionReplay = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await decisionService.getReplay(decisionId);
        loadReplay(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load replay');
      } finally {
        setLoading(false);
      }
    };

    loadDecisionReplay();
  }, [decisionId, mounted, loadReplay, setLoading, setError]);

  if (!mounted) {
    return null;
  }

  if (!decisionId) {
    return (
      <div className="container mx-auto py-8">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-900">No Decision ID</h1>
          <p className="mt-2 text-gray-600">Please provide a decision ID to replay.</p>
          <Link href="/">
            <Button className="mt-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Button>
          </Link>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="container mx-auto py-8">
        <div className="flex items-center justify-center h-64">
          <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
          <span className="ml-2 text-gray-600">Loading decision replay...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container mx-auto py-8">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-red-600">Error</h1>
          <p className="mt-2 text-gray-600">{error}</p>
          <Link href="/">
            <Button className="mt-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Button>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto py-8 px-4">
      {/* Header */}
      <div className="mb-6">
        <Link href="/">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back
          </Button>
        </Link>
        <h1 className="mt-4 text-3xl font-bold text-gray-900">Decision Replay</h1>
        <p className="mt-2 text-gray-600">Decision ID: {decisionId}</p>
      </div>

      {/* Timeline Scrubber */}
      <div className="mb-6">
        <TimelineScrubber />
      </div>

      {/* Main Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left Column - Event Inspector */}
        <div>
          <EventInspector />
        </div>

        {/* Right Column - Charts */}
        <div>
          <ChartsPanel />
        </div>
      </div>
    </div>
  );
}
