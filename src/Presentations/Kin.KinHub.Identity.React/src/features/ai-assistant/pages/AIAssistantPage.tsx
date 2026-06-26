import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Sparkles } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  RecipeAssistantProvider,
  useRecipeAssistant,
} from "@/features/ai-assistant/RecipeAssistantProvider";
import {
  RecipeBookProvider,
  useRecipeBooks,
} from "@/features/recipes/RecipeBookProvider";
import { apiClient } from "@/api/apiClient";
import type {
  AISuggestedRecipe,
  AIParsedRecipe,
  AIAdaptedRecipe,
  Fridge,
  Recipe,
  RecipeBook,
  ShoppingList,
  SuggestRecipesResult,
} from "@/types";

function SuggestTab() {
  const { t } = useTranslation();
  const { suggestRecipes, isSuggesting } = useRecipeAssistant();
  const { books } = useRecipeBooks();
  const [fridgeId, setFridgeId] = useState("");
  const [results, setResults] = useState<SuggestRecipesResult | null>(null);
  const [saveRecipe, setSaveRecipe] = useState<AISuggestedRecipe | null>(null);
  const [saveBookId, setSaveBookId] = useState("");
  const [addListRecipeKey, setAddListRecipeKey] = useState<string | null>(null);
  const [addListIds, setAddListIds] = useState<Record<string, string>>({});

  const { data: fridges = [] } = useQuery({
    queryKey: ["fridges"],
    queryFn: async () => {
      const { data } = await apiClient.get<Fridge[]>("/api/fridges");
      return data;
    },
  });

  const { data: shoppingLists = [] } = useQuery({
    queryKey: ["shopping-lists"],
    queryFn: async () => {
      const { data } = await apiClient.get<ShoppingList[]>("/api/shopping-lists");
      return data;
    },
  });

  const handleSuggest = async () => {
    const res = await suggestRecipes(fridgeId);
    setResults(res);
  };

  const handleSave = async () => {
    if (!saveRecipe || !saveBookId) return;
    try {
      await apiClient.post(
        `/api/recipe-books/${saveBookId}/recipes`,
        { ...saveRecipe.recipe, recipeBookId: saveBookId },
      );
      toast.success(t("aiAssistant.suggest.saved"));
      setSaveRecipe(null);
    } catch {
      toast.error(t("common.error"));
    }
  };

  const handleAddToList = async (key: string, names: string[]) => {
    const listId = addListIds[key];
    if (!listId || !names.length) return;
    try {
      await apiClient.post(`/api/shopping-lists/${listId}/items/bulk`, { names, shoppingListId: listId });
      toast.success(t("shoppingLists.addedToList"));
      setAddListRecipeKey(null);
    } catch {
      toast.error(t("common.error"));
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex gap-3 flex-wrap">
        <Select value={fridgeId} onValueChange={setFridgeId}>
          <SelectTrigger className="w-56">
            <SelectValue placeholder={t("aiAssistant.suggest.selectFridge")} />
          </SelectTrigger>
          <SelectContent>
            {fridges.map((f) => (
              <SelectItem key={f.id} value={f.id}>
                {f.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Button onClick={handleSuggest} disabled={!fridgeId || isSuggesting}>
          {isSuggesting
            ? t("aiAssistant.suggest.suggesting")
            : t("aiAssistant.suggest.button")}
        </Button>
      </div>

      {isSuggesting ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-40 rounded-xl" />
          ))}
        </div>
      ) : results && (
        <div className="space-y-6">
          <div>
            <h3 className="font-semibold text-sm text-muted-foreground uppercase tracking-wide mb-3">
              {t("aiAssistant.suggest.existingRecipes")}
            </h3>
            {results.existingRecipes.length === 0 ? (
              <p className="text-sm text-muted-foreground">{t("aiAssistant.suggest.noExistingMatches")}</p>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {results.existingRecipes.map((r) => (
                  <Card key={r.recipeId}>
                    <CardContent className="pt-4 pb-4">
                      <p className="font-semibold">{r.name}</p>
                      <div className="mt-3">
                        <p className="text-xs text-muted-foreground mb-1">
                          {t("aiAssistant.suggest.matchScore")}: {r.matchPercentage}%
                        </p>
                        <Progress
                          value={r.matchPercentage}
                          className="h-2 bg-muted [&>div]:bg-green-500"
                        />
                      </div>
                      {r.missingIngredients.length > 0 && (
                        <>
                          <ul className="mt-2 space-y-0.5">
                            {r.missingIngredients.map((ing, i) => (
                              <li key={i} className="text-xs text-destructive">
                                − {ing.quantity} {ing.measureUnit} {ing.name}
                              </li>
                            ))}
                          </ul>
                          {addListRecipeKey === r.recipeId ? (
                            <div className="mt-2 flex gap-2">
                              <Select
                                value={addListIds[r.recipeId] ?? ""}
                                onValueChange={(v) => setAddListIds((prev) => ({ ...prev, [r.recipeId]: v }))}
                              >
                                <SelectTrigger className="flex-1 h-8 text-xs">
                                  <SelectValue placeholder={t("shoppingLists.selectList")} />
                                </SelectTrigger>
                                <SelectContent>
                                  {shoppingLists.map((l) => (
                                    <SelectItem key={l.id} value={l.id}>{l.name}</SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                              <Button
                                size="sm"
                                className="h-8 text-xs"
                                disabled={!addListIds[r.recipeId]}
                                onClick={() => handleAddToList(r.recipeId, r.missingIngredients.map((i) => i.name))}
                              >
                                {t("shoppingLists.addToList")}
                              </Button>
                            </div>
                          ) : (
                            <Button
                              size="sm"
                              variant="outline"
                              className="mt-2 h-8 text-xs"
                              onClick={() => setAddListRecipeKey(r.recipeId)}
                            >
                              {t("shoppingLists.addToList")}
                            </Button>
                          )}
                        </>
                      )}
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </div>

          <div>
            <h3 className="font-semibold text-sm text-muted-foreground uppercase tracking-wide mb-3">
              {t("aiAssistant.suggest.aiRecipes")}
            </h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {results.newRecipes.map((r, i) => {
                const cardKey = `new-${i}`;
                return (
                  <Card key={i}>
                    <CardContent className="pt-4 pb-4">
                      <p className="font-semibold">{r.recipe.name}</p>
                      {r.recipe.backstory && (
                        <p className="text-muted-foreground text-sm mt-1">
                          {r.recipe.backstory}
                        </p>
                      )}
                      <div className="mt-3">
                        <p className="text-xs text-muted-foreground mb-1">
                          {t("aiAssistant.suggest.matchScore")}: {r.matchPercentage}%
                        </p>
                        <Progress
                          value={r.matchPercentage}
                          className="h-2 bg-muted [&>div]:bg-green-500"
                        />
                      </div>
                      {r.missingIngredients.length > 0 && (
                        <>
                          <ul className="mt-2 space-y-0.5">
                            {r.missingIngredients.map((ing, j) => (
                              <li key={j} className="text-xs text-destructive">
                                − {ing.quantity} {ing.measureUnit} {ing.name}
                              </li>
                            ))}
                          </ul>
                          {addListRecipeKey === cardKey ? (
                            <div className="mt-2 flex gap-2">
                              <Select
                                value={addListIds[cardKey] ?? ""}
                                onValueChange={(v) => setAddListIds((prev) => ({ ...prev, [cardKey]: v }))}
                              >
                                <SelectTrigger className="flex-1 h-8 text-xs">
                                  <SelectValue placeholder={t("shoppingLists.selectList")} />
                                </SelectTrigger>
                                <SelectContent>
                                  {shoppingLists.map((l) => (
                                    <SelectItem key={l.id} value={l.id}>{l.name}</SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                              <Button
                                size="sm"
                                className="h-8 text-xs"
                                disabled={!addListIds[cardKey]}
                                onClick={() => handleAddToList(cardKey, r.missingIngredients.map((ing) => ing.name))}
                              >
                                {t("shoppingLists.addToList")}
                              </Button>
                            </div>
                          ) : (
                            <Button
                              size="sm"
                              variant="outline"
                              className="mt-2 h-8 text-xs"
                              onClick={() => setAddListRecipeKey(cardKey)}
                            >
                              {t("shoppingLists.addToList")}
                            </Button>
                          )}
                        </>
                      )}
                      <Button
                        size="sm"
                        variant="outline"
                        className="mt-3"
                        onClick={() => setSaveRecipe(r)}
                      >
                        {t("aiAssistant.suggest.saveToBook")}
                      </Button>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          </div>
        </div>
      )}

      <Dialog open={!!saveRecipe} onOpenChange={() => setSaveRecipe(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("aiAssistant.suggest.saveToBook")}</DialogTitle>
          </DialogHeader>
          <Select value={saveBookId} onValueChange={setSaveBookId}>
            <SelectTrigger>
              <SelectValue placeholder={t("aiAssistant.suggest.selectBook")} />
            </SelectTrigger>
            <SelectContent>
              {books.map((b) => (
                <SelectItem key={b.id} value={b.id}>
                  {b.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <DialogFooter>
            <Button onClick={handleSave} disabled={!saveBookId}>
              {t("aiAssistant.suggest.save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function ParseTab() {
  const { t } = useTranslation();
  const { parseRecipe, isParsing } = useRecipeAssistant();
  const { books } = useRecipeBooks();
  const [rawText, setRawText] = useState("");
  const [result, setResult] = useState<AIParsedRecipe | null>(null);
  const [saveBookId, setSaveBookId] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [isSaved, setIsSaved] = useState(false);

  const handleParse = async () => {
    setIsSaved(false);
    const res = await parseRecipe(rawText);
    setResult(res);
  };

  const handleSave = async () => {
    if (!result || !saveBookId) return;
    setIsSaving(true);
    try {
      await apiClient.post(`/api/recipe-books/${saveBookId}/recipes`, { ...result, recipeBookId: saveBookId });
      setIsSaved(true);
    } catch {
      toast.error(t("common.error"));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="space-y-4">
      <Textarea
        rows={10}
        placeholder={t("aiAssistant.parse.placeholder")}
        value={rawText}
        onChange={(e) => setRawText(e.target.value)}
        className="resize-y"
      />
      <Button onClick={handleParse} disabled={!rawText || isParsing}>
        {isParsing
          ? t("aiAssistant.parse.parsing")
          : t("aiAssistant.parse.button")}
      </Button>

      {isParsing && (
        <div className="space-y-3">
          <Skeleton className="h-6 w-48" />
          <div className="grid grid-cols-2 gap-4">
            <Skeleton className="h-32" />
            <Skeleton className="h-32" />
          </div>
        </div>
      )}

      {result && (
        <Card>
          <CardContent className="pt-4 pb-4">
            <p className="font-semibold text-lg">{result.name}</p>
            {result.backstory && (
              <p className="text-muted-foreground text-sm mt-1">
                {result.backstory}
              </p>
            )}
            <div className="grid grid-cols-2 gap-6 mt-4">
              <div>
                <p className="font-medium text-sm mb-2">Ingredients</p>
                <ul className="space-y-1">
                  {result.ingredients.map((ing, i) => (
                    <li key={i} className="text-sm text-muted-foreground">
                      {ing.quantity} {ing.measureUnit} {ing.name}
                    </li>
                  ))}
                </ul>
              </div>
              <div>
                <p className="font-medium text-sm mb-2">Steps</p>
                <ol className="space-y-1">
                  {result.steps.map((step, i) => (
                    <li key={i} className="text-sm text-muted-foreground">
                      {i + 1}. {step.description}
                    </li>
                  ))}
                </ol>
              </div>
            </div>
            <div className="mt-4 flex gap-3 items-center">
              <Select value={saveBookId} onValueChange={setSaveBookId}>
                <SelectTrigger className="w-48">
                  <SelectValue
                    placeholder={t("aiAssistant.suggest.selectBook")}
                  />
                </SelectTrigger>
                <SelectContent>
                  {books.map((b) => (
                    <SelectItem key={b.id} value={b.id}>
                      {b.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {isSaved ? (
                <span className="text-sm text-green-600 dark:text-green-400 font-medium">
                  {t("aiAssistant.parse.createdSuccessfully")}
                </span>
              ) : (
                <Button onClick={handleSave} disabled={!saveBookId || isSaving}>
                  {isSaving ? t("aiAssistant.parse.saving") : t("aiAssistant.parse.saveToBook")}
                </Button>
              )}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function AdaptTab() {
  const { t } = useTranslation();
  const { adaptRecipe, isAdapting } = useRecipeAssistant();
  const [recipeId, setRecipeId] = useState("");
  const [constraint, setConstraint] = useState("");
  const [constraints, setConstraints] = useState<string[]>([]);
  const [result, setResult] = useState<AIAdaptedRecipe | null>(null);

  const { data: allRecipes = [] } = useQuery({
    queryKey: ["all-recipes"],
    queryFn: async () => {
      const { data: books } =
        await apiClient.get<RecipeBook[]>("/api/recipe-books");
      const all: (Recipe & { bookName: string })[] = [];
      for (const book of books) {
        const { data: recipes } = await apiClient.get<Recipe[]>(
          `/api/recipe-books/${book.id}/recipes`,
        );
        all.push(...recipes.map((r) => ({ ...r, bookName: book.name })));
      }
      return all;
    },
  });

  const addConstraint = () => {
    if (constraint.trim() && !constraints.includes(constraint.trim())) {
      setConstraints((c) => [...c, constraint.trim()]);
      setConstraint("");
    }
  };

  const handleAdapt = async () => {
    const res = await adaptRecipe(recipeId, constraints);
    setResult(res);
  };

  return (
    <div className="space-y-4">
      <Select value={recipeId} onValueChange={setRecipeId}>
        <SelectTrigger className="w-full max-w-sm">
          <SelectValue placeholder={t("aiAssistant.adapt.selectRecipe")} />
        </SelectTrigger>
        <SelectContent>
          {allRecipes.map((r) => (
            <SelectItem key={r.id} value={r.id}>
              {r.bookName} › {r.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <div className="flex gap-2 flex-wrap">
        <Input
          placeholder={t("aiAssistant.adapt.constraints")}
          value={constraint}
          onChange={(e) => setConstraint(e.target.value)}
          onKeyDown={(e) =>
            e.key === "Enter" && (e.preventDefault(), addConstraint())
          }
          className="max-w-xs"
        />
        <Button variant="outline" onClick={addConstraint}>
          +
        </Button>
        {constraints.map((c) => (
          <Badge
            key={c}
            variant="secondary"
            className="gap-1 cursor-pointer"
            onClick={() => setConstraints((cs) => cs.filter((x) => x !== c))}
          >
            {c} ×
          </Badge>
        ))}
      </div>

      <Button
        onClick={handleAdapt}
        disabled={!recipeId || constraints.length === 0 || isAdapting}
      >
        {isAdapting
          ? t("aiAssistant.adapt.adapting")
          : t("aiAssistant.adapt.button")}
      </Button>

      {result && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-4">
          <Card>
            <CardContent className="pt-4">
              <p className="font-semibold mb-3">
                {t("aiAssistant.adapt.original")}
              </p>
              <ul className="space-y-1">
                {result.originalRecipe.ingredients.map((ing, i) => (
                  <li
                    key={i}
                    className={`text-sm ${result.changedOriginalIngredientIds.includes(ing.id ?? '') ? "line-through text-muted-foreground" : ""}`}
                  >
                    {ing.quantity} {ing.measureUnit} {ing.name}
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4">
              <p className="font-semibold mb-3">
                {t("aiAssistant.adapt.adapted")}
              </p>
              <ul className="space-y-1">
                {result.adaptedRecipe.ingredients.map((ing, i) => (
                  <li
                    key={i}
                    className={`text-sm ${result.changedOriginalIngredientIds.includes(result.originalRecipe.ingredients[i]?.id ?? '') ? "text-green-600 dark:text-green-400" : ""}`}
                  >
                    {ing.quantity} {ing.measureUnit} {ing.name}
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}

function AIAssistantContent() {
  const { t } = useTranslation();
  return (
    <div>
      <div className="flex items-center gap-2 mb-1">
        <Sparkles className="w-6 h-6 text-primary" />
        <h1 className="text-2xl font-bold">{t("aiAssistant.title")}</h1>
      </div>
      <p className="text-muted-foreground text-sm mb-6">
        {t("aiAssistant.subtitle")}
      </p>

      <Tabs defaultValue="suggest">
        <TabsList className="w-full sm:w-auto">
          <TabsTrigger value="suggest">
            {t("aiAssistant.tabs.suggest")}
          </TabsTrigger>
          <TabsTrigger value="parse">{t("aiAssistant.tabs.parse")}</TabsTrigger>
          <TabsTrigger value="adapt">{t("aiAssistant.tabs.adapt")}</TabsTrigger>
        </TabsList>
        <div className="mt-6">
          <TabsContent value="suggest">
            <SuggestTab />
          </TabsContent>
          <TabsContent value="parse">
            <ParseTab />
          </TabsContent>
          <TabsContent value="adapt">
            <AdaptTab />
          </TabsContent>
        </div>
      </Tabs>
    </div>
  );
}

export function AIAssistantPage() {
  return (
    <RecipeAssistantProvider>
      <RecipeBookProvider>
        <AIAssistantContent />
      </RecipeBookProvider>
    </RecipeAssistantProvider>
  );
}
