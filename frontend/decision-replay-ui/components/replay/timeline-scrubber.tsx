'use client';

import { useReplayStore } from '@/lib/store';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { ChevronLeft, ChevronRight, SkipBack, SkipForward } from 'lucide-react';

export function TimelineScrubber() {
  const { events, currentEventIndex, goToEvent, nextEvent, previousEvent } = useReplayStore();

  if (events.length === 0) {
    return null;
  }

  return (
    <Card>
      <CardContent className="pt-6">
        <div className="space-y-4">
          {/* Progress Bar */}
          <div className="relative w-full h-2 bg-gray-200 rounded-full overflow-hidden">
            <div
              className="absolute h-full bg-blue-600 transition-all duration-300"
              style={{ width: `${((currentEventIndex + 1) / events.length) * 100}%` }}
            />
            {/* Event Markers */}
            <div className="absolute inset-0 flex justify-between items-center px-1">
              {events.map((_, index) => (
                <button
                  key={index}
                  onClick={() => goToEvent(index)}
                  className={`w-3 h-3 rounded-full transition-all ${
                    index === currentEventIndex
                      ? 'bg-blue-600 scale-125'
                      : index < currentEventIndex
                      ? 'bg-blue-400'
                      : 'bg-gray-300'
                  }`}
                  title={`Event ${index + 1}`}
                />
              ))}
            </div>
          </div>

          {/* Controls */}
          <div className="flex items-center justify-between">
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="icon"
                onClick={() => goToEvent(0)}
                disabled={currentEventIndex === 0}
              >
                <SkipBack className="h-4 w-4" />
              </Button>
              <Button
                variant="outline"
                size="icon"
                onClick={previousEvent}
                disabled={currentEventIndex === 0}
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button
                variant="outline"
                size="icon"
                onClick={nextEvent}
                disabled={currentEventIndex === events.length - 1}
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
              <Button
                variant="outline"
                size="icon"
                onClick={() => goToEvent(events.length - 1)}
                disabled={currentEventIndex === events.length - 1}
              >
                <SkipForward className="h-4 w-4" />
              </Button>
            </div>

            <div className="text-sm text-gray-600">
              Event {currentEventIndex + 1} of {events.length}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
