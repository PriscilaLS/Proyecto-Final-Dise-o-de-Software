-- Migracion para convertir entregas antiguas en entregas principales con versiones.
-- Ejecutar una sola vez sobre bases que ya tengan submissions con file_path/submitted_at/is_late.

USE ide_educativo;

RENAME TABLE submissions TO submissions_legacy;

CREATE TABLE submissions (
  id INT AUTO_INCREMENT PRIMARY KEY,
  task_id INT NOT NULL,
  student_id INT NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY submissions_task_student_unique (task_id, student_id),
  CONSTRAINT submissions_task_v2_fk
    FOREIGN KEY (task_id) REFERENCES tasks(id),
  CONSTRAINT submissions_student_v2_fk
    FOREIGN KEY (student_id) REFERENCES users(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE submission_versions (
  id INT AUTO_INCREMENT PRIMARY KEY,
  submission_id INT NOT NULL,
  version_number INT NOT NULL,
  file_path VARCHAR(255) NOT NULL,
  submitted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  is_late TINYINT(1) DEFAULT 0,
  UNIQUE KEY submission_versions_unique (submission_id, version_number),
  CONSTRAINT submission_versions_submission_fk
    FOREIGN KEY (submission_id) REFERENCES submissions(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO submissions (id, task_id, student_id, created_at, updated_at)
SELECT MIN(id), task_id, student_id, MIN(submitted_at), MAX(submitted_at)
FROM submissions_legacy
GROUP BY task_id, student_id;

INSERT INTO submission_versions (submission_id, version_number, file_path, submitted_at, is_late)
SELECT grouped.submission_id,
       grouped.version_number,
       grouped.file_path,
       grouped.submitted_at,
       grouped.is_late
FROM (
  SELECT s.id AS legacy_id,
         main.id AS submission_id,
         ROW_NUMBER() OVER (
           PARTITION BY s.task_id, s.student_id
           ORDER BY s.submitted_at ASC, s.id ASC
         ) AS version_number,
         s.file_path,
         s.submitted_at,
         s.is_late
  FROM submissions_legacy s
  JOIN submissions main
    ON main.task_id = s.task_id
   AND main.student_id = s.student_id
) grouped
ORDER BY grouped.submission_id, grouped.version_number;
