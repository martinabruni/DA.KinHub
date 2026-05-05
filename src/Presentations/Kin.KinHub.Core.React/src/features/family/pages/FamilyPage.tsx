import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Edit2, Lock, MoreHorizontal, Trash2, UserPlus } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
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
    isAdmin,
    updateName,
    addMember,
    removeMember,
    updateAdminCode,
    deleteFamily,
    leaveFamily,
    verifyAdminCode,
    createFamily,
  } = useFamily();

  const [editNameOpen, setEditNameOpen] = useState(false);
  const [addMemberOpen, setAddMemberOpen] = useState(false);
  const [changeCodeOpen, setChangeCodeOpen] = useState(false);
  const [codeRevealed, setCodeRevealed] = useState(false);
  const [verifyCode, setVerifyCode] = useState("");

  const nameForm = useForm<{ name: string }>({
    resolver: zodResolver(z.object({ name: z.string().min(1) })),
    defaultValues: { name: family?.name ?? "" },
  });
  const memberForm = useForm<{ name: string }>({
    resolver: zodResolver(z.object({ name: z.string().min(1) })),
    defaultValues: { name: "" },
  });
  const changeCodeForm = useForm<{ currentCode: string; newCode: string }>({
    resolver: zodResolver(
      z.object({
        currentCode: z.string().min(1),
        newCode: z.string().min(1).max(100),
      }),
    ),
    defaultValues: { currentCode: "", newCode: "" },
  });
  const createForm = useForm<{
    familyName: string;
    ownerProfileName: string;
    adminCode: string;
  }>({
    resolver: zodResolver(
      z.object({
        familyName: z.string().min(1).max(100),
        ownerProfileName: z.string().min(1).max(100),
        adminCode: z.string().min(1).max(100),
      }),
    ),
    defaultValues: { familyName: "", ownerProfileName: "", adminCode: "" },
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
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-6 max-w-sm mx-auto">
        <div className="text-center">
          <h1 className="text-2xl font-bold">{t("family.noFamily.title")}</h1>
          <p className="text-muted-foreground mt-1">
            {t("family.noFamily.description")}
          </p>
        </div>
        <form
          className="w-full space-y-3"
          onSubmit={createForm.handleSubmit(async (v) => {
            await createFamily(v);
          })}
        >
          <Input
            {...createForm.register("familyName")}
            placeholder={t("family.noFamily.familyNamePlaceholder")}
          />
          <Input
            {...createForm.register("ownerProfileName")}
            placeholder={t("family.noFamily.ownerProfileNamePlaceholder")}
          />
          <Input
            {...createForm.register("adminCode")}
            placeholder={t("family.noFamily.adminCodePlaceholder")}
          />
          <Button type="submit" className="w-full">
            {t("family.noFamily.create")}
          </Button>
        </form>
      </div>
    );
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
              {(() => {
                const adminCount = family?.members.filter(m => m.role === "Admin").length ?? 0;
                return (
                  <>
              {/* Desktop table */}
              <div className="hidden sm:block rounded-lg border overflow-hidden">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead></TableHead>
                      <TableHead>Name</TableHead>
                      <TableHead>{t("family.role.member")}</TableHead>
                      <TableHead>{t("common.actions")}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {family?.members.map((m) => {
                      const isOnlyAdmin = m.role === "Admin" && adminCount === 1;
                      return (
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
                          <Badge
                            variant={
                              m.role === "Admin" ? "default" : "secondary"
                            }
                          >
                            {m.role === "Admin"
                              ? t("family.role.admin")
                              : t("family.role.member")}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          {!isOnlyAdmin && (
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
                      );
                    })}
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
                        <Badge
                          variant={m.role === "Admin" ? "default" : "secondary"}
                          className="text-[10px]"
                        >
                          {m.role === "Admin"
                            ? t("family.role.admin")
                            : t("family.role.member")}
                        </Badge>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
                  </>
                );
              })()}
            </>
          )}
        </div>

        {/* Right sidebar */}
        <div className="space-y-4">
          {isAdmin && (
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-base flex items-center gap-2">
                  <Lock className="w-4 h-4" />
                  {t("family.adminCode.title")}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex items-center gap-2">
                  <span className="font-mono text-sm">
                    {codeRevealed ? (family?.adminCode ?? "—") : "•••••"}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setCodeRevealed((v) => !v)}
                  >
                    {codeRevealed
                      ? t("family.adminCode.hide")
                      : t("family.adminCode.reveal")}
                  </Button>
                </div>
                <div className="flex gap-2">
                  <Input
                    placeholder={t("family.adminCode.verify")}
                    value={verifyCode}
                    onChange={(e) => setVerifyCode(e.target.value)}
                    className="text-sm"
                  />
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={async () => {
                      const valid = await verifyAdminCode(verifyCode);
                      toast[valid ? "success" : "error"](
                        valid ? "Code valid" : "Invalid code",
                      );
                    }}
                  >
                    {t("family.adminCode.verifyBtn")}
                  </Button>
                </div>
                <Dialog open={changeCodeOpen} onOpenChange={setChangeCodeOpen}>
                  <DialogTrigger asChild>
                    <Button variant="destructive" size="sm" className="w-full">
                      {t("family.adminCode.changeCode")}
                    </Button>
                  </DialogTrigger>
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle>
                        {t("family.adminCode.changeCodeTitle")}
                      </DialogTitle>
                    </DialogHeader>
                    <form
                      className="space-y-3 mt-2"
                      onSubmit={changeCodeForm.handleSubmit(async (v) => {
                        await updateAdminCode(v.currentCode, v.newCode);
                        setChangeCodeOpen(false);
                        changeCodeForm.reset();
                      })}
                    >
                      <Input
                        type="password"
                        {...changeCodeForm.register("currentCode")}
                        placeholder={t(
                          "family.adminCode.currentCodePlaceholder",
                        )}
                      />
                      <Input
                        type="password"
                        {...changeCodeForm.register("newCode")}
                        placeholder={t("family.adminCode.newCodePlaceholder")}
                      />
                      <DialogFooter>
                        <Button type="submit">
                          {t("family.adminCode.changeCodeConfirm")}
                        </Button>
                      </DialogFooter>
                    </form>
                  </DialogContent>
                </Dialog>
              </CardContent>
            </Card>
          )}

          {/* Danger Zone */}
          <Card className="border-destructive/40">
            <CardHeader className="pb-2">
              <CardTitle className="text-base text-destructive">
                {t("family.dangerZone.title")}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {!isAdmin && (
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
              {isAdmin && (
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
