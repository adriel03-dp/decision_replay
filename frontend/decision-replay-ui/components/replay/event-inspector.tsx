'use client';

import { useReplayStore } from '@/lib/store';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';

export function EventInspector() {
  const { events, currentEventIndex } = useReplayStore();

  if (events.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Event Inspector</CardTitle>
          <CardDescription>No events to display</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  const currentEvent = events[currentEventIndex];

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle>Event Inspector</CardTitle>
            <CardDescription>
              {new Date(currentEvent.timestamp).toLocaleString()}
            </CardDescription>
          </div>
          <Badge variant="outline">{currentEvent.eventType}</Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Event Type */}
        <div>
          <h3 className="text-sm font-medium text-gray-500">Event Type</h3>
          <p className="mt-1 text-lg font-semibold">{currentEvent.eventType}</p>
        </div>

        <Separator />

        {/* Explanation */}
        <div>
          <h3 className="text-sm font-medium text-gray-500">Explanation</h3>
          <p className="mt-1 text-sm text-gray-700">
            {getEventExplanation(currentEvent.eventType)}
          </p>
        </div>

        <Separator />

        {/* Raw Data */}
        <div>
          <h3 className="text-sm font-medium text-gray-500 mb-2">Raw Event Data</h3>
          <pre className="bg-gray-50 p-4 rounded-lg overflow-auto text-xs">
            {JSON.stringify(currentEvent.payload, null, 2)}
          </pre>
        </div>

        {/* Metadata */}
        <div>
          <h3 className="text-sm font-medium text-gray-500 mb-2">Metadata</h3>
          <div className="space-y-1 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-500">Timestamp:</span>
              <span className="font-medium">{new Date(currentEvent.timestamp).toLocaleString()}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-500">Event Index:</span>
              <span className="font-medium">{currentEventIndex + 1} of {events.length}</span>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function getEventExplanation(eventType: string): string {
  const explanations: Record<string, string> = {
    InputCaptured: 'Initial decision input was captured with all relevant data points.',
    AIReasoningGenerated: 'AI model analyzed the input and generated reasoning with factor influences.',
    HumanOverride: 'A human decision maker reviewed and overrode the AI recommendation.',
    DecisionFinalized: 'The decision was finalized and recorded in the system.',
  };

  return explanations[eventType] || 'Event information not available.';
}
