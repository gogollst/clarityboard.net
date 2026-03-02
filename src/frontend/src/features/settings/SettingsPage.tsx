import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ProfileSection } from './components/ProfileSection';
import { PasswordSection } from './components/PasswordSection';
import { TwoFactorSection } from './components/TwoFactorSection';

export function Component() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Settings</h1>
        <p className="text-muted-foreground">
          Manage your account settings and preferences
        </p>
      </div>

      <Tabs defaultValue="profile" className="space-y-4">
        <TabsList>
          <TabsTrigger value="profile">Profile</TabsTrigger>
          <TabsTrigger value="password">Password</TabsTrigger>
          <TabsTrigger value="security">Security</TabsTrigger>
        </TabsList>

        <TabsContent value="profile">
          <ProfileSection />
        </TabsContent>

        <TabsContent value="password">
          <PasswordSection />
        </TabsContent>

        <TabsContent value="security">
          <TwoFactorSection />
        </TabsContent>
      </Tabs>
    </div>
  );
}
