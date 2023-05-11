# Copyright 2019-2021 Canaan Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
# pylint: disable=invalid-name, unused-argument, import-outside-toplevel

import pytest
import onnx
import copy
from onnx import helper
from onnx import AttributeProto, TensorProto, GraphProto
from onnx_test_runner import OnnxTestRunner
import numpy as np


def _make_module(in_shape, out_shape):
    inputs = []
    outputs = []
    initializers = []
    attributes_dict = {}

    # input
    input = helper.make_tensor_value_info('input', TensorProto.FLOAT, in_shape)
    inputs.append('input')

    shape = helper.make_tensor(
        'shape',
        TensorProto.INT64,
        dims=[len(out_shape[0])],
        vals=out_shape[0]
    )
    inputs.append('shape')
    initializers.append(shape)

    # output
    output = helper.make_tensor_value_info('output', TensorProto.FLOAT, out_shape[1])
    outputs.append('output')

    node = onnx.helper.make_node(
        'Reshape',
        inputs=inputs,
        outputs=outputs,
        **attributes_dict
    )

    nodes = []
    nodes.append(node)

    graph_def = helper.make_graph(
        nodes,
        'test-model',
        [input],
        [output],
        initializer=initializers)

    model_def = helper.make_model(graph_def, producer_name='kendryte')

    return model_def


in_shapes = [
    [1, 3, 16, 16]
]

out_shapes = [
    [[3, 256], [3, 256]],
    [[-1, 16], [48, 16]],
    [[3, 16, 16], [3, 16, 16]],
    [[0, 0, -1], [1, 3, 256]],
    [[0, 3, -1], [1, 3, 256]],
    [[0, 3, 256], [13, 256]]
]


@pytest.mark.parametrize('in_shape', in_shapes)
@pytest.mark.parametrize('out_shape', out_shapes)
def test_reshape2(in_shape, out_shape, request):
    model_def = _make_module(in_shape, out_shape)

    runner = OnnxTestRunner(request.node.name, ['k230'])
    model_file = runner.from_onnx_helper(model_def)
    runner.run(model_file)


if __name__ == "__main__":
    pytest.main(['-vv', 'test_reshape2.py'])
