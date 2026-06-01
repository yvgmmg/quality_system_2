DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = '°C'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'градус_цельсия'
    ) THEN
        EXECUTE 'ALTER TYPE public.measurement_unit RENAME VALUE ''°C'' TO ''градус_цельсия''';
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = '°'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'градус'
    ) THEN
        EXECUTE 'ALTER TYPE public.measurement_unit RENAME VALUE ''°'' TO ''градус''';
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = '%'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'процент'
    ) THEN
        EXECUTE 'ALTER TYPE public.measurement_unit RENAME VALUE ''%'' TO ''процент''';
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'м/с'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'метр_в_секунду'
    ) THEN
        EXECUTE 'ALTER TYPE public.measurement_unit RENAME VALUE ''м/с'' TO ''метр_в_секунду''';
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'м²'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'квадратный_метр'
    ) THEN
        EXECUTE 'ALTER TYPE public.measurement_unit RENAME VALUE ''м²'' TO ''квадратный_метр''';
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'м³'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'кубический_метр'
    ) THEN
        EXECUTE 'ALTER TYPE public.measurement_unit RENAME VALUE ''м³'' TO ''кубический_метр''';
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'безразм.'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_enum e
        JOIN pg_type t ON t.oid = e.enumtypid
        JOIN pg_namespace n ON n.oid = t.typnamespace
        WHERE n.nspname = 'public' AND t.typname = 'measurement_unit' AND e.enumlabel = 'безразмерная'
    ) THEN
        EXECUTE 'ALTER TYPE public.measurement_unit RENAME VALUE ''безразм.'' TO ''безразмерная''';
    END IF;
END $$;
