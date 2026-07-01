import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores([
    'dist',
    'src/features/ai-assistant/**',
    'src/features/dashboard/**',
    'src/features/family/**',
    'src/features/fridges/**',
    'src/features/profile/**',
    'src/features/recipes/**',
    'src/features/shopping-lists/**',
    'src/components/BottomNav.tsx',
    'src/components/KinConsoleServiceLayout.tsx',
    'src/components/KinRecipeServiceLayout.tsx',
    'src/components/MemberRoute.tsx',
    'src/components/MissingIngredientsAlert.tsx',
    'src/components/ServiceGuard.tsx',
    'src/components/Sidebar.tsx',
    'src/components/entity-card.tsx',
  ]),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
  },
  {
    files: [
      'src/components/ui/**/*.{ts,tsx}',
      'src/features/auth/AuthProvider.tsx',
      'src/store/authContext.tsx',
    ],
    rules: {
      'react-refresh/only-export-components': 'off',
    },
  },
])
