'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { decisionService } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Loader2, PlayCircle } from 'lucide-react';

export default function HomePage() {
  const router = useRouter();
  const [isCreating, setIsCreating] = useState(false);
  const [decisionId, setDecisionId] = useState('');
  const [formData, setFormData] = useState({
    type: 'Loan Application',
    createdBy: 'User',
    creditScore: '750',
    income: '75000',
    employmentLength: '5',
  });

  const handleCreateDecision = async () => {
    try {
      setIsCreating(true);
      
      const decision = await decisionService.createDecision({
        type: formData.type,
        createdBy: formData.createdBy,
        inputData: {
          creditScore: parseInt(formData.creditScore),
          income: parseInt(formData.income),
          employmentLength: parseInt(formData.employmentLength),
        },
      });

      // Navigate to replay page
      router.push(`/replay?id=${decision.id}`);
    } catch (error) {
      console.error('Failed to create decision:', error);
      alert('Failed to create decision. Please try again.');
    } finally {
      setIsCreating(false);
    }
  };

  const handleViewReplay = () => {
    if (decisionId.trim()) {
      router.push(`/replay?id=${decisionId}`);
    }
  };

  return (
    <div className="container mx-auto py-12 px-4">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-gray-900 mb-4">
            Decision Replay System
          </h1>
          <p className="text-xl text-gray-600">
            Visualize AI reasoning with interactive charts and timeline
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Create New Decision */}
          <Card>
            <CardHeader>
              <CardTitle>Create New Decision</CardTitle>
              <CardDescription>
                Create a decision to see AI reasoning in action
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <Label htmlFor="type">Decision Type</Label>
                <Input
                  id="type"
                  value={formData.type}
                  onChange={(e) => setFormData({ ...formData, type: e.target.value })}
                />
              </div>
              <div>
                <Label htmlFor="createdBy">Created By</Label>
                <Input
                  id="createdBy"
                  value={formData.createdBy}
                  onChange={(e) => setFormData({ ...formData, createdBy: e.target.value })}
                />
              </div>
              <div>
                <Label htmlFor="creditScore">Credit Score</Label>
                <Input
                  id="creditScore"
                  type="number"
                  value={formData.creditScore}
                  onChange={(e) => setFormData({ ...formData, creditScore: e.target.value })}
                />
              </div>
              <div>
                <Label htmlFor="income">Annual Income</Label>
                <Input
                  id="income"
                  type="number"
                  value={formData.income}
                  onChange={(e) => setFormData({ ...formData, income: e.target.value })}
                />
              </div>
              <div>
                <Label htmlFor="employmentLength">Employment Length (years)</Label>
                <Input
                  id="employmentLength"
                  type="number"
                  value={formData.employmentLength}
                  onChange={(e) => setFormData({ ...formData, employmentLength: e.target.value })}
                />
              </div>
              <Button
                onClick={handleCreateDecision}
                disabled={isCreating}
                className="w-full"
              >
                {isCreating ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Creating...
                  </>
                ) : (
                  'Create Decision'
                )}
              </Button>
            </CardContent>
          </Card>

          {/* View Existing Replay */}
          <Card>
            <CardHeader>
              <CardTitle>View Existing Replay</CardTitle>
              <CardDescription>
                Enter a decision ID to view its replay
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <Label htmlFor="decisionId">Decision ID</Label>
                <Input
                  id="decisionId"
                  placeholder="Enter decision ID (GUID)"
                  value={decisionId}
                  onChange={(e) => setDecisionId(e.target.value)}
                />
              </div>
              <Button
                onClick={handleViewReplay}
                disabled={!decisionId.trim()}
                className="w-full"
              >
                <PlayCircle className="mr-2 h-4 w-4" />
                View Replay
              </Button>

              {/* Feature Highlights */}
              <div className="mt-8 pt-8 border-t">
                <h3 className="font-semibold mb-4">Features</h3>
                <ul className="space-y-2 text-sm text-gray-600">
                  <li>✅ Timeline scrubber with event navigation</li>
                  <li>✅ Factor influence chart (horizontal bars)</li>
                  <li>✅ Risk evolution over time (line chart)</li>
                  <li>✅ AI vs Human comparison</li>
                  <li>✅ Event inspector with raw data</li>
                  <li>✅ MongoDB event sourcing</li>
                </ul>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
