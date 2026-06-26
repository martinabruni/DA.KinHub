import { ArrowRight, BookOpen, ShieldCheck, Sparkles, Users } from 'lucide-react'

const identityUrl = import.meta.env.VITE_IDENTITY_URL ?? 'http://localhost:5174'
const kinRecipeUrl = import.meta.env.VITE_KINRECIPE_URL ?? 'http://localhost:5175'

function App() {
  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(255,255,255,0.9),_rgba(244,238,228,0.92)_42%,_rgba(224,212,194,0.98))] text-slate-950">
      <div className="mx-auto flex min-h-screen max-w-6xl flex-col px-6 py-10 md:px-10">
        <div className="mb-12 flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-3xl">
            <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-slate-900/10 bg-white/80 px-3 py-1 text-xs font-semibold uppercase tracking-[0.22em] text-slate-600 shadow-sm">
              <Sparkles className="h-3.5 w-3.5" />
              Family operating system
            </div>
            <h1 className="max-w-4xl font-serif text-5xl font-semibold leading-none tracking-tight md:text-7xl">
              KinHub now routes work into focused apps instead of one crowded shell.
            </h1>
            <p className="mt-5 max-w-2xl text-base leading-7 text-slate-700 md:text-lg">
              Identity owns access, family, and service management. KinRecipe owns meal planning,
              fridges, shopping lists, and assisted cooking flows.
            </p>
          </div>
          <div className="grid gap-3 rounded-[2rem] border border-white/70 bg-white/70 p-5 shadow-[0_18px_60px_rgba(76,56,35,0.12)] backdrop-blur">
            <div className="flex items-center gap-3 text-sm text-slate-600">
              <ShieldCheck className="h-5 w-5 text-emerald-600" />
              Backend split compiled successfully
            </div>
            <div className="flex items-center gap-3 text-sm text-slate-600">
              <Users className="h-5 w-5 text-sky-700" />
              Identity and KinRecipe frontends separated
            </div>
          </div>
        </div>

        <div className="grid flex-1 gap-6 lg:grid-cols-[1.1fr_0.9fr]">
          <section className="rounded-[2rem] border border-slate-900/8 bg-[#f6efe4] p-6 shadow-[0_24px_80px_rgba(76,56,35,0.12)] md:p-8">
            <div className="mb-10 flex items-center justify-between gap-4">
              <div>
                <p className="text-sm font-medium uppercase tracking-[0.2em] text-slate-500">
                  Launch point
                </p>
                <h2 className="mt-2 text-3xl font-semibold tracking-tight text-slate-900">
                  Choose the flow you need
                </h2>
              </div>
            </div>

            <div className="grid gap-5">
              <a
                href={identityUrl}
                className="group rounded-[1.75rem] border border-slate-900/10 bg-white p-6 transition hover:-translate-y-0.5 hover:shadow-xl"
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-700">
                      <ShieldCheck className="h-6 w-6" />
                    </div>
                    <h3 className="text-2xl font-semibold tracking-tight">Identity</h3>
                    <p className="mt-3 max-w-xl text-sm leading-6 text-slate-600">
                      Sign in, create accounts, pick a family member, manage family settings,
                      and control which services are active.
                    </p>
                  </div>
                  <ArrowRight className="mt-1 h-5 w-5 shrink-0 text-slate-400 transition group-hover:translate-x-1 group-hover:text-slate-900" />
                </div>
              </a>

              <a
                href={kinRecipeUrl}
                className="group rounded-[1.75rem] border border-slate-900/10 bg-[#fbf7f1] p-6 transition hover:-translate-y-0.5 hover:shadow-xl"
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-100 text-amber-700">
                      <BookOpen className="h-6 w-6" />
                    </div>
                    <h3 className="text-2xl font-semibold tracking-tight">KinRecipe</h3>
                    <p className="mt-3 max-w-xl text-sm leading-6 text-slate-600">
                      Jump into recipe books, ingredient tracking, shopping lists, and AI-assisted
                      cooking. If you are not authenticated, KinRecipe will send you through
                      Identity first.
                    </p>
                  </div>
                  <ArrowRight className="mt-1 h-5 w-5 shrink-0 text-slate-400 transition group-hover:translate-x-1 group-hover:text-slate-900" />
                </div>
              </a>
            </div>
          </section>

          <aside className="rounded-[2rem] border border-slate-900/8 bg-slate-950 p-6 text-slate-50 shadow-[0_24px_80px_rgba(20,20,20,0.24)] md:p-8">
            <p className="text-sm font-medium uppercase tracking-[0.2em] text-slate-400">
              Operating model
            </p>
            <h2 className="mt-3 text-3xl font-semibold tracking-tight">
              One hub, two domains of responsibility.
            </h2>
            <div className="mt-8 grid gap-4">
              <div className="rounded-[1.5rem] border border-white/10 bg-white/5 p-5">
                <p className="text-sm font-semibold text-white">Identity app</p>
                <p className="mt-2 text-sm leading-6 text-slate-300">
                  Auth, member context, service activation, and profile maintenance stay here.
                </p>
              </div>
              <div className="rounded-[1.5rem] border border-white/10 bg-white/5 p-5">
                <p className="text-sm font-semibold text-white">KinRecipe app</p>
                <p className="mt-2 text-sm leading-6 text-slate-300">
                  Recipe workflows are isolated so the experience can evolve without carrying
                  family-management concerns in the same UI shell.
                </p>
              </div>
              <div className="rounded-[1.5rem] border border-white/10 bg-gradient-to-br from-amber-300/20 to-transparent p-5">
                <p className="text-sm font-semibold text-white">Current checkpoint</p>
                <p className="mt-2 text-sm leading-6 text-slate-300">
                  The backend split is verified. Frontend routing now reflects the new boundaries,
                  and cross-app session relay is wired for the KinRecipe handoff.
                </p>
              </div>
            </div>
          </aside>
        </div>
      </div>
    </main>
  )
}

export default App
