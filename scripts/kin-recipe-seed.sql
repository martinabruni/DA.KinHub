-- =============================================================================
-- KinHub - Test data for kinrecipe schema
-- FamilyId: cd6443f2-d7d3-49b6-bb30-dbced7a763e5
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- Variables (UUID fissi per idempotenza)
-- ---------------------------------------------------------------------------

-- RecipeBook IDs
-- book_nonna  = 'a1000000-0000-0000-0000-000000000001'
-- book_veloci = 'a1000000-0000-0000-0000-000000000002'

-- Recipe IDs
-- recipe_pomodoro = 'b1000000-0000-0000-0000-000000000001'
-- recipe_risotto  = 'b1000000-0000-0000-0000-000000000002'
-- recipe_tiramisu = 'b1000000-0000-0000-0000-000000000003'
-- recipe_cecipasta= 'b1000000-0000-0000-0000-000000000004'

-- Fridge ID
-- fridge_main     = 'c1000000-0000-0000-0000-000000000001'

-- ---------------------------------------------------------------------------
-- RecipeBookEntity
-- ---------------------------------------------------------------------------

INSERT INTO kinrecipe."RecipeBookEntity"
    ("Id", "Name", "Description", "FamilyId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    (
        'a1000000-0000-0000-0000-000000000001',
        'Ricette della Nonna',
        'Le ricette tradizionali di famiglia, tramandate di generazione in generazione.',
        'cd6443f2-d7d3-49b6-bb30-dbced7a763e5',
        FALSE,
        '2026-01-10T10:00:00Z',
        '2026-01-10T10:00:00Z'
    ),
    (
        'a1000000-0000-0000-0000-000000000002',
        'Piatti Veloci',
        'Ricette semplici e veloci per i giorni di fretta, pronte in meno di 30 minuti.',
        'cd6443f2-d7d3-49b6-bb30-dbced7a763e5',
        FALSE,
        '2026-02-05T09:00:00Z',
        '2026-02-05T09:00:00Z'
    );

-- ---------------------------------------------------------------------------
-- RecipeEntity
-- ---------------------------------------------------------------------------

INSERT INTO kinrecipe."RecipeEntity"
    ("Id", "Name", "Backstory", "FinalTime", "Portions", "RecipeBookId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    (
        'b1000000-0000-0000-0000-000000000001',
        'Pasta al Pomodoro',
        'Il classico della cucina italiana. La nonna la faceva ogni domenica con i pomodori dell''orto.',
        INTERVAL '30 minutes',
        4,
        'a1000000-0000-0000-0000-000000000001',
        FALSE,
        '2026-01-10T10:05:00Z',
        '2026-01-10T10:05:00Z'
    ),
    (
        'b1000000-0000-0000-0000-000000000002',
        'Risotto ai Funghi Porcini',
        'Una ricetta autunnale ricca di profumo di bosco. I funghi porcini secchi fanno tutta la differenza.',
        INTERVAL '45 minutes',
        4,
        'a1000000-0000-0000-0000-000000000001',
        FALSE,
        '2026-01-15T11:00:00Z',
        '2026-01-15T11:00:00Z'
    ),
    (
        'b1000000-0000-0000-0000-000000000003',
        'Tiramisù',
        'Il dolce simbolo della famiglia. Richiede riposo in frigo, ma il risultato vale sempre l''attesa.',
        INTERVAL '4 hours',
        6,
        'a1000000-0000-0000-0000-000000000001',
        FALSE,
        '2026-01-20T14:00:00Z',
        '2026-01-20T14:00:00Z'
    ),
    (
        'b1000000-0000-0000-0000-000000000004',
        'Pasta e Ceci',
        'Piatto povero della tradizione romana, economico e nutriente. Pronto in pochissimo tempo.',
        INTERVAL '25 minutes',
        4,
        'a1000000-0000-0000-0000-000000000002',
        FALSE,
        '2026-02-05T09:10:00Z',
        '2026-02-05T09:10:00Z'
    );

-- ---------------------------------------------------------------------------
-- RecipeIngredientEntity
-- ---------------------------------------------------------------------------

-- Pasta al Pomodoro
INSERT INTO kinrecipe."RecipeIngredientEntity"
    ("Id", "Name", "MeasureUnit", "Quantity", "RecipeId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    ('d1000000-0000-0000-0001-000000000001', 'Spaghetti',             'g',   320, 'b1000000-0000-0000-0000-000000000001', FALSE, '2026-01-10T10:05:00Z', '2026-01-10T10:05:00Z'),
    ('d1000000-0000-0000-0001-000000000002', 'Pomodori pelati',       'g',   400, 'b1000000-0000-0000-0000-000000000001', FALSE, '2026-01-10T10:05:00Z', '2026-01-10T10:05:00Z'),
    ('d1000000-0000-0000-0001-000000000003', 'Aglio',                 'pz',    2, 'b1000000-0000-0000-0000-000000000001', FALSE, '2026-01-10T10:05:00Z', '2026-01-10T10:05:00Z'),
    ('d1000000-0000-0000-0001-000000000004', 'Olio extravergine',     'ml',   40, 'b1000000-0000-0000-0000-000000000001', FALSE, '2026-01-10T10:05:00Z', '2026-01-10T10:05:00Z'),
    ('d1000000-0000-0000-0001-000000000005', 'Basilico fresco',       'foglie', 6, 'b1000000-0000-0000-0000-000000000001', FALSE, '2026-01-10T10:05:00Z', '2026-01-10T10:05:00Z');

-- Risotto ai Funghi Porcini
INSERT INTO kinrecipe."RecipeIngredientEntity"
    ("Id", "Name", "MeasureUnit", "Quantity", "RecipeId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    ('d1000000-0000-0000-0002-000000000001', 'Riso Carnaroli',        'g',   320, 'b1000000-0000-0000-0000-000000000002', FALSE, '2026-01-15T11:00:00Z', '2026-01-15T11:00:00Z'),
    ('d1000000-0000-0000-0002-000000000002', 'Funghi porcini secchi', 'g',    30, 'b1000000-0000-0000-0000-000000000002', FALSE, '2026-01-15T11:00:00Z', '2026-01-15T11:00:00Z'),
    ('d1000000-0000-0000-0002-000000000003', 'Brodo vegetale',        'ml',  900, 'b1000000-0000-0000-0000-000000000002', FALSE, '2026-01-15T11:00:00Z', '2026-01-15T11:00:00Z'),
    ('d1000000-0000-0000-0002-000000000004', 'Parmigiano Reggiano',   'g',    60, 'b1000000-0000-0000-0000-000000000002', FALSE, '2026-01-15T11:00:00Z', '2026-01-15T11:00:00Z'),
    ('d1000000-0000-0000-0002-000000000005', 'Burro',                 'g',    30, 'b1000000-0000-0000-0000-000000000002', FALSE, '2026-01-15T11:00:00Z', '2026-01-15T11:00:00Z');

-- Tiramisù
INSERT INTO kinrecipe."RecipeIngredientEntity"
    ("Id", "Name", "MeasureUnit", "Quantity", "RecipeId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    ('d1000000-0000-0000-0003-000000000001', 'Savoiardi',             'g',   300, 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z'),
    ('d1000000-0000-0000-0003-000000000002', 'Mascarpone',            'g',   500, 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z'),
    ('d1000000-0000-0000-0003-000000000003', 'Uova',                  'pz',    4, 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z'),
    ('d1000000-0000-0000-0003-000000000004', 'Zucchero semolato',     'g',   100, 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z'),
    ('d1000000-0000-0000-0003-000000000005', 'Caffè espresso',        'ml',  300, 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z'),
    ('d1000000-0000-0000-0003-000000000006', 'Cacao amaro in polvere','g',    20, 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z');

-- Pasta e Ceci
INSERT INTO kinrecipe."RecipeIngredientEntity"
    ("Id", "Name", "MeasureUnit", "Quantity", "RecipeId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    ('d1000000-0000-0000-0004-000000000001', 'Pasta corta (ditalini)', 'g',  280, 'b1000000-0000-0000-0000-000000000004', FALSE, '2026-02-05T09:10:00Z', '2026-02-05T09:10:00Z'),
    ('d1000000-0000-0000-0004-000000000002', 'Ceci in scatola',        'g',  400, 'b1000000-0000-0000-0000-000000000004', FALSE, '2026-02-05T09:10:00Z', '2026-02-05T09:10:00Z'),
    ('d1000000-0000-0000-0004-000000000003', 'Rosmarino fresco',       'rametti', 2, 'b1000000-0000-0000-0000-000000000004', FALSE, '2026-02-05T09:10:00Z', '2026-02-05T09:10:00Z'),
    ('d1000000-0000-0000-0004-000000000004', 'Aglio',                  'pz',   2, 'b1000000-0000-0000-0000-000000000004', FALSE, '2026-02-05T09:10:00Z', '2026-02-05T09:10:00Z'),
    ('d1000000-0000-0000-0004-000000000005', 'Olio extravergine',      'ml',  40, 'b1000000-0000-0000-0000-000000000004', FALSE, '2026-02-05T09:10:00Z', '2026-02-05T09:10:00Z');

-- ---------------------------------------------------------------------------
-- RecipeStepEntity
-- ---------------------------------------------------------------------------

-- Pasta al Pomodoro
INSERT INTO kinrecipe."RecipeStepEntity"
    ("Id", "Order", "Description", "RecipeId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    ('e1000000-0000-0000-0001-000000000001', 1, 'Portare a ebollizione abbondante acqua salata.', 'b1000000-0000-0000-0000-000000000001', FALSE, '2026-01-10T10:05:00Z', '2026-01-10T10:05:00Z'),
    ('e1000000-0000-0000-0001-000000000002', 2, 'In una padella larga, scaldare l''olio con l''aglio schiacciato a fuoco medio finché dorato, poi rimuovere l''aglio.', 'b1000000-0000-0000-0000-000000000001', FALSE, '2026-01-10T10:05:00Z', '2026-01-10T10:05:00Z'),
    ('e1000000-0000-0000-0001-000000000003', 3, 'Aggiungere i pomodori pelati, schiacciandoli con un cucchiaio. Cuocere a fuoco medio-basso per 15 minuti. Aggiustare di sale.', 'b1000000-0000-0000-0000-000000000001', FALSE, '2026-01-10T10:05:00Z', '2026-01-10T10:05:00Z'),
    ('e1000000-0000-0000-0001-000000000004', 4, 'Cuocere gli spaghetti al dente, scolarli e saltarli nella padella con il sugo per 1 minuto. Servire con foglie di basilico fresco.', 'b1000000-0000-0000-0000-000000000001', FALSE, '2026-01-10T10:05:00Z', '2026-01-10T10:05:00Z');

-- Risotto ai Funghi Porcini
INSERT INTO kinrecipe."RecipeStepEntity"
    ("Id", "Order", "Description", "RecipeId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    ('e1000000-0000-0000-0002-000000000001', 1, 'Mettere in ammollo i funghi porcini in acqua tiepida per 20 minuti, poi scolarli conservando l''acqua filtrata.', 'b1000000-0000-0000-0000-000000000002', FALSE, '2026-01-15T11:00:00Z', '2026-01-15T11:00:00Z'),
    ('e1000000-0000-0000-0002-000000000002', 2, 'In una casseruola larga, sciogliere 20g di burro e tostare il riso per 2 minuti finché traslucido.', 'b1000000-0000-0000-0000-000000000002', FALSE, '2026-01-15T11:00:00Z', '2026-01-15T11:00:00Z'),
    ('e1000000-0000-0000-0002-000000000003', 3, 'Aggiungere i funghi e sfumare con un mestolo di brodo caldo. Continuare ad aggiungere brodo un mestolo alla volta, mescolando, per circa 18 minuti.', 'b1000000-0000-0000-0000-000000000002', FALSE, '2026-01-15T11:00:00Z', '2026-01-15T11:00:00Z'),
    ('e1000000-0000-0000-0002-000000000004', 4, 'A fuoco spento, mantecare con il burro rimanente e il Parmigiano Reggiano. Coprire e attendere 2 minuti prima di servire.', 'b1000000-0000-0000-0000-000000000002', FALSE, '2026-01-15T11:00:00Z', '2026-01-15T11:00:00Z');

-- Tiramisù
INSERT INTO kinrecipe."RecipeStepEntity"
    ("Id", "Order", "Description", "RecipeId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    ('e1000000-0000-0000-0003-000000000001', 1, 'Separare i tuorli dagli albumi. Montare i tuorli con lo zucchero fino ad ottenere un composto chiaro e spumoso.', 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z'),
    ('e1000000-0000-0000-0003-000000000002', 2, 'Incorporare il mascarpone ai tuorli montati, mescolando delicatamente.', 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z'),
    ('e1000000-0000-0000-0003-000000000003', 3, 'Montare gli albumi a neve ferma e incorporarli alla crema con movimenti dal basso verso l''alto.', 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z'),
    ('e1000000-0000-0000-0003-000000000004', 4, 'Inzuppare velocemente i savoiardi nel caffè freddo e disporli in una pirofila. Coprire con metà della crema, fare un secondo strato di savoiardi e coprire con la crema rimanente.', 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z'),
    ('e1000000-0000-0000-0003-000000000005', 5, 'Spolverare con cacao amaro e riporre in frigorifero per almeno 3 ore prima di servire.', 'b1000000-0000-0000-0000-000000000003', FALSE, '2026-01-20T14:00:00Z', '2026-01-20T14:00:00Z');

-- Pasta e Ceci
INSERT INTO kinrecipe."RecipeStepEntity"
    ("Id", "Order", "Description", "RecipeId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    ('e1000000-0000-0000-0004-000000000001', 1, 'In una pentola, soffriggere l''aglio con l''olio e il rosmarino a fuoco medio per 2 minuti.', 'b1000000-0000-0000-0000-000000000004', FALSE, '2026-02-05T09:10:00Z', '2026-02-05T09:10:00Z'),
    ('e1000000-0000-0000-0004-000000000002', 2, 'Aggiungere i ceci scolati e 600ml di acqua. Portare a bollore.', 'b1000000-0000-0000-0000-000000000004', FALSE, '2026-02-05T09:10:00Z', '2026-02-05T09:10:00Z'),
    ('e1000000-0000-0000-0004-000000000003', 3, 'Versare la pasta direttamente nel brodo, cuocere secondo i tempi di cottura. Aggiustare di sale e aggiungere acqua calda se necessario. Servire con un filo d''olio a crudo.', 'b1000000-0000-0000-0000-000000000004', FALSE, '2026-02-05T09:10:00Z', '2026-02-05T09:10:00Z');

-- ---------------------------------------------------------------------------
-- FridgeEntity
-- ---------------------------------------------------------------------------

INSERT INTO kinrecipe."FridgeEntity"
    ("Id", "Name", "FamilyId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    (
        'c1000000-0000-0000-0000-000000000001',
        'Dispensa di Casa',
        'cd6443f2-d7d3-49b6-bb30-dbced7a763e5',
        FALSE,
        '2026-03-01T08:00:00Z',
        '2026-03-01T08:00:00Z'
    );

-- ---------------------------------------------------------------------------
-- FridgeIngredientEntity
-- ---------------------------------------------------------------------------

INSERT INTO kinrecipe."FridgeIngredientEntity"
    ("Id", "Name", "MeasureUnit", "Quantity", "FridgeId", "IsDeleted", "CreatedAt", "UpdatedAt")
VALUES
    ('f1000000-0000-0000-0000-000000000001', 'Spaghetti',             'g',   500, 'c1000000-0000-0000-0000-000000000001', FALSE, '2026-03-01T08:00:00Z', '2026-03-01T08:00:00Z'),
    ('f1000000-0000-0000-0000-000000000002', 'Pomodori pelati',       'g',   800, 'c1000000-0000-0000-0000-000000000001', FALSE, '2026-03-01T08:00:00Z', '2026-03-01T08:00:00Z'),
    ('f1000000-0000-0000-0000-000000000003', 'Uova',                  'pz',    6, 'c1000000-0000-0000-0000-000000000001', FALSE, '2026-03-01T08:00:00Z', '2026-03-01T08:00:00Z'),
    ('f1000000-0000-0000-0000-000000000004', 'Mascarpone',            'g',   250, 'c1000000-0000-0000-0000-000000000001', FALSE, '2026-03-01T08:00:00Z', '2026-03-01T08:00:00Z'),
    ('f1000000-0000-0000-0000-000000000005', 'Parmigiano Reggiano',   'g',   150, 'c1000000-0000-0000-0000-000000000001', FALSE, '2026-03-01T08:00:00Z', '2026-03-01T08:00:00Z'),
    ('f1000000-0000-0000-0000-000000000006', 'Olio extravergine',     'ml',  500, 'c1000000-0000-0000-0000-000000000001', FALSE, '2026-03-01T08:00:00Z', '2026-03-01T08:00:00Z'),
    ('f1000000-0000-0000-0000-000000000007', 'Ceci in scatola',       'g',   400, 'c1000000-0000-0000-0000-000000000001', FALSE, '2026-03-01T08:00:00Z', '2026-03-01T08:00:00Z'),
    ('f1000000-0000-0000-0000-000000000008', 'Aglio',                 'pz',    5, 'c1000000-0000-0000-0000-000000000001', FALSE, '2026-03-01T08:00:00Z', '2026-03-01T08:00:00Z'),
    ('f1000000-0000-0000-0000-000000000009', 'Burro',                 'g',   100, 'c1000000-0000-0000-0000-000000000001', FALSE, '2026-03-01T08:00:00Z', '2026-03-01T08:00:00Z'),
    ('f1000000-0000-0000-0000-000000000010', 'Riso Carnaroli',        'g',   500, 'c1000000-0000-0000-0000-000000000001', FALSE, '2026-03-01T08:00:00Z', '2026-03-01T08:00:00Z');

COMMIT;