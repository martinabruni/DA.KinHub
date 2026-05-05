import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Edit2, MoreHorizontal, Trash2, UserPlus } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { FamilyProvider, useFamily } from "@/features/family/FamilyProvider";
import { getInitials } from "@/lib/utils";

function FamilyContent() {
  const { t } = useTranslation();
  const {
    family,
    isLoading,
    updateName,
    addMember,
    removeMember,
    deleteFamily,
    leaveFamily,
  } = useFamily();

  const [editNameOpen, setEditNameOpen] = useState(false);
  const [addMemberOpen, setAddMemberOpen] = useState(false);

  const nameForm = useForm<{ name: string }>({
    resolver: zodResolver(z.object({ name: z.string().min(1) })),
    defaultValues: { name: family?.name ?? "" },
  });
  const memberForm = useForm<{ name: string }>({
    resolver: zodResolver(z.object({ name: z.string().min(1) })),
    defaultValues: { name: "" },
  });

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64 w-full rounded-xl" />
      </div>
    );
  }

  if (!family) {
    return null;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <h1 className="text-2xl font-bold">
          {family?.name ?? t("family.title")}
        </h1>
        <Dialog open={editNameOpen} onOpenChange={setEditNameOpen}>
          <DialogTrigger asChild>
            <Button variant="ghost" size="icon">
              <Edit2 className="w-4 h-4" />
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>{t("family.editName")}</DialogTitle>
            </DialogHeader>
            <form
              onSubmit={nameForm.handleSubmit(async (v) => {
                await updateName(v.name);
                setEditNameOpen(false);
              })}
            >
              <Input {...nameForm.register("name")} className="mt-2" />
              <DialogFooter className="mt-4">
                <Button type="submit">{t("family.save")}</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Members */}
        <div className="lg:col-span-2 space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold">{t("family.members")}</h2>
            <Dialog open={addMemberOpen} onOpenChange={setAddMemberOpen}>
              <DialogTrigger asChild>
                <Button variant="outline" size="sm">
                  <UserPlus className="w-4 h-4 mr-1" />
                  {t("family.addMember")}
                </Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>{t("family.addMember")}</DialogTitle>
                </DialogHeader>
                <form
                  onSubmit={memberForm.handleSubmit(async (v) => {
                    await addMember(v.name);
                    setAddMemberOpen(false);
                  })}
                >
                  <Input
                    type="text"
                    {...memberForm.register("name")}
                    placeholder={t("family.addMemberNamePlaceholder")}
                    className="mt-2"
                  />
                  <DialogFooter className="mt-4">
                    <Button type="submit">{t("family.save")}</Button>
                  </DialogFooter>
                </form>
              </DialogContent>
            </Dialog>
          </div>

          {(family?.members?.length ?? 0) === 0 ? (
            <p className="text-muted-foreground text-sm">
              {t("family.noMembers")}
            </p>
          ) : (
            <>
              {/* Desktop table */}
              <div className="hidden sm:block rounded-lg border overflow-hidden">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead></TableHead>
                      <TableHead>Name</TableHead>
                      <TableHead>{t("common.actions")}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {family?.members.map((m) => (
                      <TableRow key={m.id}>
                        <TableCell>
                          <Avatar className="w-8 h-8">
                            <AvatarFallback className="text-xs">
                              {getInitials(m.name)}
                            </AvatarFallback>
                          </Avatar>
                        </TableCell>
                        <TableCell className="font-medium">{m.name}</TableCell>
                        <TableCell>
                          {(family?.members.length ?? 0) > 1 && (
                            <AlertDialog>
                              <DropdownMenu>
                                <DropdownMenuTrigger asChild>
                                  <Button variant="ghost" size="icon">
                                    <MoreHorizontal className="w-4 h-4" />
                                  </Button>
                                </DropdownMenuTrigger>
                                <DropdownMenuContent>
                                  <AlertDialogTrigger asChild>
                                    <DropdownMenuItem className="text-destructive">
                                      <Trash2 className="w-4 h-4 mr-2" />
                                      {t("family.actions.remove")}
                                    </DropdownMenuItem>
                                  </AlertDialogTrigger>
                                </DropdownMenuContent>
                              </DropdownMenu>
                              <AlertDialogContent>
                                <AlertDialogHeader>
                                  <AlertDialogTitle>
                                    {t("family.removeMember.title")}
                                  </AlertDialogTitle>
                                  <AlertDialogDescription>
                                    {t("family.removeMember.description", {
                                      name: m.name,
                                    })}
                                  </AlertDialogDescription>
                                </AlertDialogHeader>
                                <AlertDialogFooter>
                                  <AlertDialogCancel>
                                    {t("common.cancel")}
                                  </AlertDialogCancel>
                                  <AlertDialogAction
                                    onClick={() => removeMember(m.id)}
                                  >
                                    {t("family.removeMember.confirm")}
                                  </AlertDialogAction>
                                </AlertDialogFooter>
                              </AlertDialogContent>
                            </AlertDialog>
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
              {/* Mobile cards */}
              <div className="sm:hidden space-y-2">
                {family?.members.map((m) => (
                  <Card key={m.id}>
                    <CardContent className="flex items-center gap-3 p-3">
                      <Avatar className="w-9 h-9">
                        <AvatarFallback className="text-sm">
                          {getInitials(m.name)}
                        </AvatarFallback>
                      </Avatar>
                      <div className="flex-1">
                        <p className="text-sm font-medium">{m.name}</p>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </>
          )}
        </div>

        {/* Right sidebar */}
        <div className="space-y-4">
          {/* Danger Zone */}
          <Card className="border-destructive/40">
            <CardHeader className="pb-2">
              <CardTitle className="text-base text-destructive">
                {t("family.dangerZone.title")}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {(family?.members.length ?? 0) > 1 && (
                <AlertDialog>
                  <AlertDialogTrigger asChild>
                    <Button
                      variant="outline"
                      className="w-full border-destructive text-destructive hover:bg-destructive/10"
                    >
                      {t("family.dangerZone.leave")}
                    </Button>
                  </AlertDialogTrigger>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>
                        {t("family.dangerZone.leaveTitle")}
                      </AlertDialogTitle>
                      <AlertDialogDescription>
                        {t("family.dangerZone.leaveDescription")}
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>
                        {t("common.cancel")}
                      </AlertDialogCancel>
                      <AlertDialogAction onClick={leaveFamily}>
                        {t("family.dangerZone.leaveConfirm")}
                      </AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              )}
              {(family?.members.length ?? 0) > 1 && (
                <AlertDialog>
                  <AlertDialogTrigger asChild>
                    <Button variant="destructive" className="w-full">
                      {t("family.dangerZone.delete")}
                    </Button>
                  </AlertDialogTrigger>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>
                        {t("family.dangerZone.deleteTitle")}
                      </AlertDialogTitle>
                      <AlertDialogDescription>
                        {t("family.dangerZone.deleteDescription")}
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>
                        {t("common.cancel")}
                      </AlertDialogCancel>
                      <AlertDialogAction
                        onClick={deleteFamily}
                        className="bg-destructive"
                      >
                        {t("family.dangerZone.deleteConfirm")}
                      </AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

export function FamilyPage() {
  return (
    <FamilyProvider>
      <FamilyContent />
    </FamilyProvider>
  );
}
