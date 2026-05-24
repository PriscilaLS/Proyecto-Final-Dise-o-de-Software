<?php
require_once __DIR__ . '/../Models/baseModel.php';

abstract class BaseRepository {
    protected BaseModel $model;

    public function __construct(BaseModel $model) {
        $this->model = $model;
    }

    public function findById(int $id): ?array {
        return $this->model->findById($id);
    }

    abstract public function save(array $data): int;
    abstract public function findAll(): array;
}
